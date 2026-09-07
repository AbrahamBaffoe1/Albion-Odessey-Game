using UnityEngine;

namespace AlbionOdyssey
{
    public sealed class Explorer : MonoBehaviour
    {
        public Camera eyes;
        public CharacterController body;
        public bool controls=true;
        float pitch,velocity;
        public bool pointerControls;
        public Vector2 buttonMove;
        public bool buttonJump;
        public float buttonTurn;
        void Update()
        {
            if(!controls)return;
            if(Input.GetKeyDown(KeyCode.Escape)){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            if(Input.GetMouseButtonDown(0)&&!pointerControls){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
            if(Cursor.lockState!=CursorLockMode.Locked&&!pointerControls)return;
            if(Cursor.lockState==CursorLockMode.Locked)
            {
            transform.Rotate(0,Input.GetAxisRaw("Mouse X")*2,0);
            pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*2,-85,85);
            eyes.transform.localRotation=Quaternion.Euler(pitch,0,0);
            }
            float x=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0)+buttonMove.x;
            float z=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)+buttonMove.y;
            Vector3 move=Vector3.ClampMagnitude(transform.right*x+transform.forward*z,1)*(Input.GetKey(KeyCode.LeftShift)?6.5f:3.8f);
            if(body.isGrounded&&velocity<0)velocity=-2;
            if(pointerControls)transform.Rotate(0,((Input.GetKey(KeyCode.X)?1:0)-(Input.GetKey(KeyCode.Z)?1:0)+buttonTurn)*90*Time.deltaTime,0);
            if(body.isGrounded&&(Input.GetKeyDown(KeyCode.Space)||buttonJump))velocity=5.5f;
            buttonJump=false;
            velocity-=18*Time.deltaTime;
            body.Move((move+Vector3.up*velocity)*Time.deltaTime);
            if(transform.position.y < -10)Teleport(new Vector3(0,.05f,-22));
        }
        public void Teleport(Vector3 position)
        {
            body.enabled=false;transform.position=position;body.enabled=true;velocity=0;
        }
    }
}
