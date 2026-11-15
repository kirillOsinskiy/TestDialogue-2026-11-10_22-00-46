using System.Threading;
using System.Threading.Tasks;

namespace Game.Dialogue
{
    public interface IDialogueScreenService
    {
        /// <summary>
        /// Открывает экран и завершается только после его закрытия.
        /// При отмене реализация должна прекратить ожидание
        /// и корректно освободить связанный с диалогом UI.
        /// </summary>
        Task OpenAsync(
            string screenId,
            CancellationToken cancellationToken);
    }
}