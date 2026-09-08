using UnityEngine;
namespace AlbionOdyssey
{
    [DefaultExecutionOrder(100)]
    public sealed class Explorer : MonoBehaviour
    {
        public Camera eyes; public CharacterController body; public bool controls=true;
        public bool pointerControls,thirdPerson=true; public Vector2 buttonMove;public bool buttonJump;public float buttonTurn;
        public KeeperAvatar avatar; public CampusCar vehicle; public float cameraDistance=4.5f;
        float pitch=12,velocity,movementSpeed;
        public void CreateAvatar()
        {
            var model=new GameObject("Visible Keeper");model.transform.SetParent(transform,false);avatar=model.AddComponent<KeeperAvatar>();avatar.Build(1,0,0,true);
        }
        void Update()
        {
            if(avatar!=null)avatar.Animate(controls?movementSpeed:0,vehicle!=null);
            if(!controls)return;
            if(Input.GetMouseButtonDown(0)&&!pointerControls){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
            if(Cursor.lockState!=CursorLockMode.Locked&&!pointerControls)return;
            if(Input.GetKeyDown(KeyCode.V))thirdPerson=!thirdPerson;
            if(Cursor.lockState==CursorLockMode.Locked)
            {
                if(vehicle==null)transform.Rotate(0,Input.GetAxisRaw("Mouse X")*2,0);
                pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*2,-45,65);
            }
            float x=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0)+buttonMove.x;
            float z=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)+buttonMove.y;
            if(vehicle!=null){vehicle.Drive(Mathf.Clamp(z,-1,1),Mathf.Clamp(x,-1,1),Input.GetKey(KeyCode.Space)||buttonJump,Time.deltaTime);buttonJump=false;movementSpeed=0;return;}
            Vector3 move=Vector3.ClampMagnitude(transform.right*x+transform.forward*z,1)*(Input.GetKey(KeyCode.LeftShift)?6.5f:3.8f);movementSpeed=move.magnitude;
            if(body.isGrounded&&velocity<0)velocity=-2;
            if(pointerControls)transform.Rotate(0,((Input.GetKey(KeyCode.X)?1:0)-(Input.GetKey(KeyCode.Z)?1:0)+buttonTurn)*90*Time.deltaTime,0);
            if(body.isGrounded&&(Input.GetKeyDown(KeyCode.Space)||buttonJump))velocity=5.5f;
            buttonJump=false;velocity-=18*Time.deltaTime;var before=transform.position;body.Move((move+Vector3.up*velocity)*Time.deltaTime);
            var delta=transform.position-before;delta.y=0;movementSpeed=delta.magnitude/Mathf.Max(.001f,Time.deltaTime);
            if(avatar!=null&&move.sqrMagnitude>.01f&&thirdPerson)avatar.transform.localRotation=Quaternion.RotateTowards(avatar.transform.localRotation,Quaternion.Euler(0,Mathf.Atan2(x,z)*Mathf.Rad2Deg,0),540*Time.deltaTime);
            if(transform.position.y < -10)Teleport(new Vector3(0,.05f,-22));
        }
        void LateUpdate()
        {
            if(eyes==null||avatar==null)return;
            // The old physics walkthrough owns its camera for its explicit captures.
            if(OdysseySmoke.Enabled&&!controls&&!thirdPerson)return;
            UpdateCamera();
        }
        public void UpdateCamera()
        {
            Quaternion look=transform.rotation*Quaternion.Euler(pitch,0,0);
            if(!thirdPerson&&vehicle==null){eyes.transform.localPosition=new Vector3(0,1.65f,0);eyes.transform.rotation=look;avatar.gameObject.SetActive(false);return;}
            Vector3 target=transform.position+Vector3.up*(vehicle==null?1.35f:2f);
            Vector3 offset=look*new Vector3(.42f,.4f,-(vehicle==null?cameraDistance:8));
            bool bodyEnabled=body.enabled;body.enabled=false;if(vehicle!=null)vehicle.hull.enabled=false;
            if(Physics.SphereCast(target,.18f,offset.normalized,out var hit,offset.magnitude,~0,QueryTriggerInteraction.Ignore))offset=offset.normalized*Mathf.Max(.15f,hit.distance-.1f);
            body.enabled=bodyEnabled;if(vehicle!=null)vehicle.hull.enabled=true;
            eyes.transform.position=target+offset;eyes.transform.rotation=Quaternion.LookRotation(target+look*Vector3.forward*5-eyes.transform.position,Vector3.up);
            avatar.gameObject.SetActive(offset.magnitude>.75f);
        }
        public bool TryTarget(out RaycastHit hit)
        {
            bool enabled=body.enabled;body.enabled=false;
            bool found=Physics.Raycast(eyes.transform.position,eyes.transform.forward,out hit,thirdPerson?10.5f:3.5f)&&Vector3.Distance(transform.position,hit.point)<4.5f;
            body.enabled=enabled;return found;
        }
        public bool TryExitVehicle()
        {
            if(vehicle==null)return true;vehicle.speed=0;return vehicle.Exit();
        }
        public void Teleport(Vector3 position)
        {
            if(vehicle!=null&&!TryExitVehicle())return;
            body.enabled=false;transform.position=position;body.enabled=true;velocity=0;
        }
    }
}
