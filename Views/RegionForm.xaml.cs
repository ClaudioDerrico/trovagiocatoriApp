using trovagiocatoriApp.ViewModels;

namespace trovagiocatoriApp.Views;

public partial class RegionForm : ContentPage
{
    public RegionForm()
    {
        InitializeComponent();
        BindingContext = new RegionFormViewModel();
    }
}
