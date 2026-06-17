using System;
using Terminals.Data;

namespace Terminals.Forms
{
    internal interface IConnectionCommands
    {
        event EventHandler ConnectionStateChanged;

        void Disconnect();

        void Reconnect();

        bool CanExecute(IFavorite selected);
    }
}