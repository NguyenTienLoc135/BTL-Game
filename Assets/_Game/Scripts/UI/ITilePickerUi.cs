using System;
using System.Collections.Generic;

namespace BTLGame.UI
{
    /// <summary>
    /// Chọn ô trực tiếp trên bàn cờ (dịch chuyển): không popup che bàn, người chơi nhìn bàn rồi bấm vào ô muốn tới.
    /// View chỉ phát ý định; host kiểm tra hợp lệ và gửi lệnh cho Core.
    /// </summary>
    public interface ITilePickerUi
    {
        event Action<int> TileChosen;
        event Action SkipRequested;
        event Action OtherActionsRequested;

        void ShowTilePicker(string message, IReadOnlyList<int> tileIds, bool canSkip, bool hasOtherActions);
        void HideTilePicker();
    }
}
