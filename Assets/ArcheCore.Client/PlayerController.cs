
using ArcheCore.Client.Networking;
using ArcheCore.Client.Networking.C2W;
using ArcheCore.Client.Networking.W2C;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArchCore.Client
{
    public class PlayerController : MonoBehaviour
    {
        public bool isLocalPlayer;

        public int networkId;

        [SerializeField]
        private float moveSpeed = 5f;

        private Vector3 targetPosition;

        private void Start()
        {
            targetPosition =
                transform.position;
        }

        private void Update()
        {
            if (isLocalPlayer)
            {
                HandleLocalMovement();
            }
            else
            {
                HandleRemoteMovement();
            }
        }

        private void HandleLocalMovement()
        {
            Vector2 input =
                Keyboard.current != null
                    ? new Vector2(
                        (Keyboard.current.dKey.isPressed ? 1f : 0f) -
                        (Keyboard.current.aKey.isPressed ? 1f : 0f),

                        (Keyboard.current.wKey.isPressed ? 1f : 0f) -
                        (Keyboard.current.sKey.isPressed ? 1f : 0f))
                    : Vector2.zero;

            Vector3 movement =
                new Vector3(
                    input.x,
                    0f,
                    input.y)
                * moveSpeed
                * Time.deltaTime;

            transform.Translate(
                movement,
                Space.World);

            if (movement != Vector3.zero)
            {
                C2WPlayerMovePacket.Send(
                    ClientNetwork.Instance.ServerPeer,
                    transform.position);
            }
        }

        private void HandleRemoteMovement()
        {
            transform.position =
                Vector3.Lerp(
                    transform.position,
                    targetPosition,
                    Time.deltaTime * 10f);
        }

        public void SetTargetPosition(
            Vector3 position)
        {
            targetPosition =
                position;
        }
    }
}