/*
 * Lone EFT DMA Radar
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using System.Windows.Input;

namespace LoneEftDmaRadar.UI.Misc
{
    public sealed class InputBoxViewModel : INotifyPropertyChanged
    {
        public string Title { get; }
        public string Prompt { get; }
        private string _inputText;
        public string InputText
        {
            get => _inputText;
            set
            {
                if (value == _inputText) return;
                _inputText = value;
                OnPropertyChanged(nameof(InputText));
            }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        // Fires when either command runs; subscriber should close the window
        public event EventHandler<CloseRequestedEventArgs> CloseRequested;

        public InputBoxViewModel(string title, string prompt)
        {
            Title = title;
            Prompt = prompt;
            OkCommand = new SimpleCommand(() => OnCloseRequested(true));
            CancelCommand = new SimpleCommand(() => OnCloseRequested(false));
        }

        private void OnCloseRequested(bool result)
            => CloseRequested?.Invoke(this, new CloseRequestedEventArgs(result));

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public class CloseRequestedEventArgs : EventArgs
        {
            public bool DialogResult { get; }
            public CloseRequestedEventArgs(bool dialogResult) => DialogResult = dialogResult;
        }
    }
}
