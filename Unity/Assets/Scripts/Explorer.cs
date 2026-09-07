using UnityEngine;

namespace AlbionOdyssey
{
    public sealed class Explorer : MonoBehaviour
    {
        public Camera eyes;
        public CharacterController body;
        public bool controls=true;
        float pitch,velocity;
        void Update()
        {
            if(!controls)return;
            if(Input.GetKeyDown(KeyCode.Escape)){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            if(Input.GetMouseButtonDown(0)){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
            if(Cursor.lockState!=CursorLockMode.Locked)return;
            transform.Rotate(0,Input.GetAxisRaw("Mouse X")*2,0);
            pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*2,-85,85);
            eyes.transform.localRotation=Quaternion.Euler(pitch,0,0);
            float x=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);
            float z=(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);
            Vector3 move=Vector3.ClampMagnitude(transform.right*x+transform.forward*z,1)*(Input.GetKey(KeyCode.LeftShift)?6.5f:3.8f);
            if(body.isGrounded&&velocity<0)velocity=-2;
            if(body.isGrounded&&Input.GetKeyDown(KeyCode.Space))velocity=5.5f;
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
