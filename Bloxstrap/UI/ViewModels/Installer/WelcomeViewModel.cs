namespace Bloxstrap.UI.ViewModels.Installer
{
    public class WelcomeViewModel : NotifyPropertyChangedViewModel
    {
        // formatting is done here instead of in xaml, it's just a bit easier
        public string MainText => String.Format(
            Strings.Installer_Welcome_MainText,
            "[github.com/mel0k1/SakuraTrap](https://github.com/mel0k1/SakuraTrap)",
            "[github.com/mel0k1/SakuraTrap/releases](https://github.com/mel0k1/SakuraTrap/releases)"
        );

        public bool CanContinue { get; set; } = false;
    }
}
