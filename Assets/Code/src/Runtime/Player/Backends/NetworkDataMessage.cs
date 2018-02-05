﻿using UnityEngine;
using UnityEngine.Networking;

namespace HouraiTeahouse.FantasyCrescendo.Networking {

public struct NetworkDataMessage {

  public readonly NetworkConnection Connection;
  public readonly NetworkReader NetworkReader;

  public NetworkDataMessage(NetworkConnection connection, NetworkReader reader) {
    Connection = connection;
    NetworkReader = reader;
  }

  public T ReadAs<T>() where T : MessageBase, new() {
    var message = ObjectPool<T>.Shared.Rent();
    message.Deserialize(NetworkReader);
    return message;
  }

}

}