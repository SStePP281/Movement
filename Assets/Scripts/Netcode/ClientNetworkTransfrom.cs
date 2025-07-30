using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities.ClientAuthority
{
    public enum AuthorityMode
    {
        Server,
        Client
    }

    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        public AuthorityMode authorityMode = AuthorityMode.Client;
        protected override bool OnIsServerAuthoritative()
        {
            return authorityMode == AuthorityMode.Server;
        }
    }
}