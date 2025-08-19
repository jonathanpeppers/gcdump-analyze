namespace hellomauileak;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // Run some GCs, to make .gcdump have less objects
        for (int i = 0; i < 3; i++)
            GC.Collect();
    }

    private async void OnCounterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LeakyPage());
    }
}
