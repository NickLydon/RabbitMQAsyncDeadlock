# RabbitMQAsyncDeadlock

Publish some messages to queue `TestQueue` and then see that an exception is raised:
```
System.TimeoutException: The operation has timed out.
   at RabbitMQ.Util.BlockingCell.GetValue(TimeSpan timeout)
   at RabbitMQ.Client.Impl.SimpleBlockingRpcContinuation.GetReply(TimeSpan timeout)
   at RabbitMQ.Client.Impl.ModelBase.ModelRpc(MethodBase method, ContentHeaderBase header, Byte[] body)
   at RabbitMQ.Client.Framing.Impl.Model._Private_ChannelOpen(String outOfBand)
   at RabbitMQ.Client.Framing.Impl.AutorecoveringConnection.CreateNonRecoveringModel()
   at RabbitMQ.Client.Framing.Impl.AutorecoveringConnection.CreateModel()
   at RabbitMQAsyncDeadlock.ConnectionTest.OnMessageReceivedAsync(IConnection connection, IModel channel, BasicDeliverEventArgs received) in C:\Dev\RabbitMQAsyncDeadlock\RabbitMQAsyncDeadlock\Program.cs:line 99
```

To fix it, uncomment [this line](https://github.com/NickLydon/RabbitMQAsyncDeadlock/blob/master/RabbitMQAsyncDeadlock/Program.cs#L93)
