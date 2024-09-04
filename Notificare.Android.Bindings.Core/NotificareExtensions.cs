using Java.Lang;
using Kotlin.Coroutines;

namespace RE.Notifica;

/// <summary>
/// This class contains extensions to use the Android Binding library more conveniently.
/// </summary>
public class NotificareExtensions
{
    /// <summary>
    /// Allows awaiting an SDK method which uses the <see cref="INotificareCallback"/> to inform about the completion of an asynchronous operation.
    /// </summary>
    /// <param name="suspend">A lambda accepting the <see cref="INotificareCallback"/> to pass to the async function in the SDK.</param>
    /// <returns>A task, which completes when the asynchronous operation completed.</returns>
    public static async Task<Java.Lang.Object?> AwaitCallback(Action<INotificareCallback> suspend)
    {
        var x = new AwaitNotificareCallback();
        suspend(x);
        return await x.Task;
    }
    /// <summary>
    /// Allows awaiting an SDK method which uses the <see cref="INotificareCallback"/> to inform about the completion of an asynchronous operation.
    /// </summary>
    /// <param name="suspend">A lambda accepting the <see cref="INotificareCallback"/> to pass to the async function in the SDK.</param>
    /// <returns>A task, which completes when the asynchronous operation completed.</returns>
    public static async Task<Java.Lang.Object?> AwaitSuspend(Action<IContinuation> suspend)
    {
        var x = new AwaitKotlinContinuation();
        suspend(x);
        return await x.Task;
    }

    private class AwaitKotlinContinuation : Java.Lang.Object, IContinuation
    {
        private readonly TaskCompletionSource<Java.Lang.Object?> _taskCompletionSource = new();

        public Task<Java.Lang.Object?> Task => _taskCompletionSource.Task;

        public void ResumeWith(Java.Lang.Object result)
        {
            _taskCompletionSource.TrySetResult(result);
        }

        public ICoroutineContext Context => EmptyCoroutineContext.Instance;
    }

    private class AwaitNotificareCallback : Java.Lang.Object, INotificareCallback
    {
        private readonly TaskCompletionSource<Java.Lang.Object?> _taskCompletionSource = new();
        public Task<Java.Lang.Object?> Task => _taskCompletionSource.Task;

        public void OnFailure(Java.Lang.Exception e)
        {
            _taskCompletionSource.TrySetException(Throwable.ToException(e));
        }

        public void OnSuccess(Java.Lang.Object? result)
        {
            _taskCompletionSource.TrySetResult(result);
        }
    }

}