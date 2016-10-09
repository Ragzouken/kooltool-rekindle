using System;
using System.Collections.Generic;

public class MessageHandler
{
    private Dictionary<Type, Action<object>> handlers
        = new Dictionary<Type, Action<object>>();

    public void SetHandler<TMessage>(Action<TMessage> handler)
    {
        handlers.Add(typeof(TMessage), message => handler((TMessage) message));
    }

    public bool Handle<TMessage>(TMessage message)
    {
        Action<object> handler;

        if (handlers.TryGetValue(typeof(TMessage), out handler))
        {
            handler(message);

            return true;
        }
        else
        {
            return false;
        }
    }
}
