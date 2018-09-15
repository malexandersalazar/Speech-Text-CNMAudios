using CSCore;
using CSCore.Codecs;
using ES.SpeechToText.CNMAudios.Classes;
using Microsoft.CognitiveServices.Speech;
using Microsoft.ProjectOxford.Text.KeyPhrase;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace ES.SpeechToText.CNMAudios
{
    public partial class MainWindow : Window
    {
        #region View Variables

        public ObservableCollection<CNMAudioItem> _cnmAudios = new ObservableCollection<CNMAudioItem>();
        public DateTime? _lastAudioSent = null;

        #endregion View Variables

        #region Cognitive Services Variables

        private SpeechFactory _speechFactory = null;
        private KeyPhraseClient _keyPhraseClient = null;

        #endregion Cognitive Services Variables

        public MainWindow()
        {
            InitializeComponent();
            CNMAudiosListBox.ItemsSource = _cnmAudios;
        }

        private void Add_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "wav files (*.wav)|*.wav";
            if (ofd.ShowDialog() == true)
            {
                var cnmAudioItem = new CNMAudioItem();
                cnmAudioItem.IsBusy = true;
                cnmAudioItem.Code = Guid.NewGuid().ToString();
                cnmAudioItem.Filename = string.Format("{0}.wav", cnmAudioItem.Code);
                cnmAudioItem.OriginalFilename = ofd.FileName;
                _cnmAudios.Add(cnmAudioItem);

                Task.Factory.StartNew(() => StartProcessWithNewAudio(cnmAudioItem));
                _lastAudioSent = DateTime.Now;
            }
        }

        private async void StartProcessWithNewAudio(CNMAudioItem cnmAudioItem)
        {
            if (_lastAudioSent != null && (DateTime.Now - _lastAudioSent.Value).TotalMilliseconds < 12000)
                await Task.Delay(12000 - (int)(DateTime.Now - _lastAudioSent.Value).TotalMilliseconds);

            ResampleAudio(cnmAudioItem.OriginalFilename, cnmAudioItem.Filename);
            await RecognizeSpeechAsync(cnmAudioItem);
            DetectKeyPhrases(cnmAudioItem);
        }

        private void ResampleAudio(string originFilename, string destinyFilename)
        {
            using (var source = CodecFactory.Instance.GetCodec(originFilename)
                .ToSampleSource()
                .ChangeSampleRate(16000)
                .ToMono()
                .ToWaveSource(16))
            {
                source.WriteToFile(destinyFilename);
            }
        }

        public async Task RecognizeSpeechAsync(CNMAudioItem cnmAudioItem)
        {
            var stopRecognition = new TaskCompletionSource<int>();

            var factory = _speechFactory ?? SpeechFactory.FromSubscription("77623f52633c426890a6d2bb11116c8b", "westus");
            using (var recognizer = factory.CreateSpeechRecognizerWithFileInput(cnmAudioItem.Filename, "es-ES"))
            {
                recognizer.FinalResultReceived += (s, e) =>
                {
                    if (e.Result.RecognitionStatus == RecognitionStatus.Recognized)
                        cnmAudioItem.AppendTextLine(e.Result.Text);
                };
                recognizer.OnSessionEvent += (s, e) =>
                {
                    if (e.EventType == SessionEventType.SessionStoppedEvent)
                    {
                        cnmAudioItem.IsBusy = false;
                        stopRecognition.TrySetResult(0);
                    }
                };
                recognizer.RecognitionErrorRaised += (s, e) =>
                {
                    Console.WriteLine(e.FailureReason);
                };

                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                await stopRecognition.Task.ConfigureAwait(false);
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
        }

        public async void DetectKeyPhrases(CNMAudioItem cnmAudioItem)
        {
            if (string.IsNullOrWhiteSpace(cnmAudioItem.RecognizedText))
                return;

            var keyPhraseClient = _keyPhraseClient ?? new KeyPhraseClient("4f2c3df7714f4bbfbc742547624a6f2b");
            var keyPhraseRequest = new KeyPhraseRequest();
            keyPhraseRequest.Documents.Add(new KeyPhraseDocument { Id = Guid.NewGuid().ToString(), Text = cnmAudioItem.RecognizedText, Language = "es" });
            var keyPhraseResult = await keyPhraseClient.GetKeyPhrasesAsync(keyPhraseRequest);
            if (keyPhraseResult.Errors.Count == 0)
            {
                var doc = keyPhraseResult.Documents[0];
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    cnmAudioItem.KeyPhrases = doc.KeyPhrases;
                }), null).Wait();
            }
        }
    }
}