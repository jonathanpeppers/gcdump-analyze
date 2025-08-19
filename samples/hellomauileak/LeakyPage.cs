namespace hellomauileak;

class LeakyPage : ContentPage
{
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        Window.PropertyChanged += Window_PropertyChanged;
    }

    private void Window_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
    }
}
