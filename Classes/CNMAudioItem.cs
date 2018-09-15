using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ES.SpeechToText.CNMAudios.Classes
{
    public class CNMAudioItem : INotifyPropertyChanged
    {
        public string Code { get; set; }
        public string Filename { get; set; }
        public string OriginalFilename { get; set; }
        public string RecognizedText { get; private set; }
        public List<string> _keyPhrases;
        public List<string> KeyPhrases { get { return _keyPhrases; } set { _keyPhrases = value; RaisePropertyChanged(); } }
        private bool _isBusy;
        public bool IsBusy { get { return _isBusy; } set { _isBusy = value; RaisePropertyChanged(); } }
        public void AppendTextLine(string line)
        {
            RecognizedText = string.Concat(RecognizedText, Environment.NewLine, line);
            RaisePropertyChanged("RecognizedText");
        }
        public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}