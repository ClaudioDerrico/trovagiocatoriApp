using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace trovagiocatoriApp.ViewModels;

/// <summary>
/// Classe base per tutti i ViewModel.
/// Gestisce l'implementazione di INotifyPropertyChanged per il binding.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Metodo per notificare i cambiamenti di proprietà.
    /// </summary>
    /// <param name="propertyName">Il nome della proprietà che è cambiata.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Imposta il valore di una proprietà e notifica il cambiamento.
    /// </summary>
    /// <typeparam name="T">Il tipo della proprietà.</typeparam>
    /// <param name="storage">La variabile di archiviazione.</param>
    /// <param name="value">Il nuovo valore della proprietà.</param>
    /// <param name="propertyName">Il nome della proprietà.</param>
    /// <returns>True se il valore è cambiato; altrimenti false.</returns>
    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
