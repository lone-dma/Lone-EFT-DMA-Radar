using System.Windows.Input;

namespace EftDmaRadarLite.UI.Misc
{
    public class SimpleCommand : ICommand
    {
        private readonly Action _execute;
        public SimpleCommand(Action execute) => _execute = execute;
        public bool CanExecute(object _) => true;
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
        public void Execute(object _) => _execute();
    }
}
