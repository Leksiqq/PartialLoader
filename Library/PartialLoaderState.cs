namespace Net.Leksi.PartialLoader;

/// <summary>
/// <para xml:lang="ru">
/// Информирует о текущем состоянии объекта через свойство 
/// <see cref="PartialLoader{T}.State"/>
/// </para>
/// <para xml:lang="en">
/// Informs about the current state of the object via the property 
/// <see cref="PartialLoader{T}.State"/>
/// </para>
/// </summary>
public enum PartialLoaderState 
{
    /// <summary>
    /// <para xml:lang="ru">
    /// Объект <see cref="PartialLoader{T}"/> был создан или был вызван 
    /// метод <see cref="PartialLoader{T}.Reset"/>
    /// </para>
    /// <para xml:lang="en">
    /// The object <see cref="PartialLoader{T}"/> has been created or 
    /// the method <see cref="PartialLoader{T}.Reset"/> has been called
    /// </para>
    /// </summary>
    New,


    /// <summary>
    /// <para xml:lang="ru">
    /// Был вызван метод <see cref="PartialLoader{T}.LoadAsync"/> в первый 
    /// раз и ещё не вернул управление вызывающему коду
    /// </para>
    /// <para xml:lang="en">
    /// The <see cref="PartialLoader{T}.LoadAsync"/> method was called for 
    /// the first time and has not yet returned to the caller
    /// </para>
    /// </summary>
    Started,

    /// <summary>
    /// <para xml:lang="ru">
    /// Метод <see cref="PartialLoader{T}.LoadAsync"/> вернул управление 
    /// вызывающему коду, данные успешно получены, но не все
    /// </para>
    /// <para xml:lang="en">
    /// The <see cref="PartialLoader{T}.LoadAsync"/> method returned control 
    /// to the calling code, the data was received successfully, but not all
    /// </para>
    /// </summary>
    Partial,

    /// <summary>
    /// <para xml:lang="ru">
    /// Был вызван метод <see cref="PartialLoader{T}.LoadAsync"/> в очередной 
    /// раз и ещё не вернул управление вызывающему коду
    /// </para>
    /// <para xml:lang="en">
    /// The <see cref="PartialLoader{T}.LoadAsync"/> method has been called 
    /// yet again and has not yet returned control to the calling code
    /// </para>
    /// </summary>
    Continued,

    /// <summary>
    /// <para xml:lang="ru">
    /// Метод <see cref="PartialLoader{T}.LoadAsync"/> вернул управление 
    /// вызывающему коду, все данные успешно получены
    /// </para>
    /// <para xml:lang="en">
    /// Method <see cref="PartialLoader{T}.LoadAsync"/> returned control 
    /// to the calling code, all data was received successfully
    /// </para>
    /// </summary>
    Full,

    /// <summary>
    /// <para xml:lang="ru">
    /// Работа объекта была прервана либо вызовом метода 
    /// <see cref="PartialLoader{T}.Cancel"/>, либо через внешний 
    /// <see cref="CancellationTokenSource"/>
    /// </para>
    /// <para xml:lang="en">
    /// The operation of the object was interrupted either by a call 
    /// to the <see cref="PartialLoader{T}.Cancel"/> method, or via 
    /// an external <see cref="CancellationTokenSource"/>
    /// </para>
    /// </summary>
    Canceled
}

