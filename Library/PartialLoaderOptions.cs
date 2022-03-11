namespace Net.Leksi.PartialLoader;

/// <summary>
/// <para xml:lang="ru">
/// Класс для передачи настроек в объект <see cref="PartialLoader{T}"/>
/// </para>
/// <para xml:lang="en">
/// Class for passing settings to the object <see cref="PartialLoader{T}"/>
/// </para>
/// </summary>
public class PartialLoaderOptions
{
    /// <summary>
    /// <para xml:lang="ru">
    /// Значение интервала, по истечении которого происходит возврат 
    /// в вызывающий код из метода <see cref="PartialLoader{T}.LoadAsync"/>.
    /// Неположительное значение означает, что такой интервал не установлен.
    /// </para>
    /// <para xml:lang="en">
    /// The value of the interval after which the return to the calling code 
    /// from the <see cref="PartialLoader{T}.LoadAsync"/> method occurs.
    /// A non-positive value  value means that such an interval is not set.
    /// </para>
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromTicks(0);

    /// <summary>
    /// <para xml:lang="ru">
    /// Значение количества полученных объектов, по достижении которого 
    /// происходит возврат в вызывающий код из метода 
    /// <see cref="PartialLoader{T}.LoadAsync"/>.
    /// Неположительное значение означает, что соответствующее значение 
    /// не установлено.
    /// </para>
    /// <para xml:lang="en">
    /// The value of the number of received objects, upon reaching which 
    /// the caller returns from 
    /// the <see cref="PartialLoader{T}.LoadAsync"/> method.
    /// A non-positive value means that the corresponding value has not been set.
    /// </para>
    /// </summary>
    public int Paging { get; set; } = 0;

    /// <summary>
    /// <para xml:lang="ru">
    /// Значение, применяемое ко всем await-вызовам
    /// </para>
    /// <para xml:lang="en">
    /// Value applied to all await calls
    /// </para>
    /// </summary>
    public bool ConfigureAwait { get; set; } = false;

    /// <summary>
    /// <para xml:lang="ru">
    /// <see cref="CancellationToken"/> передаваемый из внешнего кода
    /// </para>
    /// <para xml:lang="en">
    /// <see cref="CancellationToken"/> passed from external code
    /// </para>
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

}

