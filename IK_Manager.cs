#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

//copy it first, and then build it such that you only need it for chainlength=2

public class IK_Manager : MonoBehaviour
{
    public int Chainlength = 2;

    public Transform Target;
    public Transform Pole;

    [Header("Solver Parameters")]
    public int Iterations = 10; //solver iterations per update
    public float Delta = .001f;//Distance when solver stops

    [Range(0, 1)]
    public float SnapBackStrength = 1f;

    protected float[] BonesLength; //Target to Origin
    protected float CompleteLength;
    protected Transform[] Bones;
    protected Vector3[] Positions;
    protected Vector3[] StartDirectionSucc;
    protected Quaternion[] StartRotationBone;
    protected Quaternion StartRotationTarget;
    public Transform Root; //information about a transform

    //  root
    //  (bone0) (bonelen 0) (bone1) (bonelen 1) (bone2)...
    //   x--------------------x--------------------x---...

    void Awake()
    {
        Init();
    }

    void Init()
    {
        Bones = new Transform[Chainlength + 1];
        Positions = new Vector3[Chainlength + 1];
        BonesLength = new float[Chainlength];
        StartDirectionSucc = new Vector3[Chainlength + 1];
        StartRotationBone = new Quaternion[Chainlength + 1];

        Root = transform;//see it equal to the current instance of the transform
        //since this script is at the end piece, and transform is specific to children
        for (var i = 0; i <= Chainlength; i++)
        {
            if (Root == null)
                throw new UnityException("The chain value is longer than the ancestor chain!");
            Root = Root.parent; //set the root for each in the chainlength
        }

        if (Target == null)
        {
            Target = new GameObject(gameObject.name + "Target").transform;//?
            SetPositionRootSpace(Target, GetPositionRootSpace(transform));
        }
        StartRotationTarget = GetRotationRootSpace(Target);

        //init data
        var current = transform;
        CompleteLength = 0;
        for (var i = Bones.Length - 1; i >= 0; i--)
        {
            Bones[i] = current; //each object in the chain, 0=shoulder, 3=finger
            StartRotationBone[i] = GetRotationRootSpace(current);

            if (i == Bones.Length - 1)
            {
                //Vector from the end of the chain to the target
                StartDirectionSucc[i] = GetPositionRootSpace(Target) - GetPositionRootSpace(current);
            }
            else
            {
                //Vector from current bone to the next
                StartDirectionSucc[i] = GetPositionRootSpace(Bones[i + 1]) - GetPositionRootSpace(current);
                //BonesLength[i] = StartDirectionSucc[i].magnitude;
                BonesLength[i] = StartDirectionSucc[i].magnitude;
                CompleteLength += BonesLength[i];
            }
            current = current.parent;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //get position
        for (int i = 0; i < Bones.Length; i++)
        {
            //inverse of the roots rotation*vectorbetween current joint and root?
            //position[0]=shoulder
            Positions[i] = GetPositionRootSpace(Bones[i]);
        }
      
        //Distance between root and target greater than total length?
        if ((Target.position - GetPositionRootSpace(Bones[0])).sqrMagnitude >= CompleteLength * CompleteLength)
        {
            var direction = (Target.position - Positions[0]).normalized;
            for (int i = 1; i < Positions.Length; i++)
                //set each bone to be bonelength away from the other along a direct vector to the target
                Positions[i] = Positions[i - 1] + direction * BonesLength[i - 1];
        }
        else
        {
            for (int i = 0; i < Positions.Length - 1; i++)
                //child=snap strength% between last parent position+StartDirectionSucc and last self(child) position
                Positions[i + 1] = Vector3.Lerp(Positions[i + 1], Positions[i] + StartDirectionSucc[i], SnapBackStrength);

            //do it until "close enough" break below
            for (int iteration = 0; iteration < Iterations; iteration++) 
            {
                //backward, from extremity to origin
                for (int i = Positions.Length - 1; i > 0; i--)
                {
                    if (i == Positions.Length - 1)
                        Positions[i] = Target.position; //places last bone on target
                    else
                        //Set other positions to be bonelength away from their child
                        Positions[i] = Positions[i + 1] + (Positions[i] - Positions[i + 1]).normalized * BonesLength[i];
                }

                //forward(from origin out to extremity
                for (int i = 1; i < Positions.Length; i++)
                    Positions[i] = Positions[i - 1] + (Positions[i] - Positions[i - 1]).normalized * BonesLength[i - 1];

                //close enough?
                if ((Positions[Positions.Length - 1] - Target.position).sqrMagnitude < Delta * Delta)
                    break;
            }
        }

        if (Pole != null)//move towards pole
        {
            var polePosition = GetPositionRootSpace(Pole);
            for (int i = 1; i < Positions.Length - 1; i++)
            {
                //define plane
                var plane = new Plane(Positions[i + 1] - Positions[i - 1], Positions[i - 1]);
                var projectedPole = plane.ClosestPointOnPlane(polePosition);
                var projectedBone = plane.ClosestPointOnPlane(Positions[i]);
                var angle = Vector3.SignedAngle(projectedBone - Positions[i - 1], projectedPole - Positions[i - 1], plane.normal);
                Positions[i] = Quaternion.AngleAxis(angle, plane.normal) * (Positions[i] - Positions[i - 1]) + Positions[i - 1];
            }
        }
        //FINALLY set position and rotation with SetRootSpace functions
        // start rotations?
        for (int i = 0; i < Positions.Length; i++)//cycle through each position
        {
            //Amplify the rotation of the root by some crazy quaternion stuff
            if (i == Positions.Length - 1)
                SetRotationRootSpace(Bones[i], Quaternion.Inverse(Target.rotation) * StartRotationTarget
                    * Quaternion.Inverse(StartRotationBone[i]));
            else
                SetRotationRootSpace(Bones[i], Quaternion.FromToRotation(StartDirectionSucc[i],
                    Positions[i + 1] - Positions[i]) * Quaternion.Inverse(StartRotationBone[i]));
            //Set position of each bone to be Root.rotation * Position[i] + Root.position
            SetPositionRootSpace(Bones[i], Positions[i]);
        }
    }

    private Vector3 GetPositionRootSpace(Transform current)
    {
        if (Root == null)
            return current.position;
        else
            //
            return Quaternion.Inverse(Root.rotation) * (current.position - Root.position);
    }

    private void SetPositionRootSpace(Transform current,Vector3 position)
    {
        if (Root == null)
        {
            current.position = position;
        }       
        else
        {
            current.position = Root.rotation * position + Root.position;
        }
    }

    private Quaternion GetRotationRootSpace(Transform current)
    {
        if (Root ==null)
        {
            return current.rotation;
        }
        else
        {
            return Quaternion.Inverse(current.rotation) * Root.rotation;
        }
    }

    private void SetRotationRootSpace(Transform current,Quaternion rotation)
    {
        if (Root == null)
        {
            current.rotation = rotation;
        }
        else
        {
            current.rotation = Root.rotation * rotation;
        }    
    }

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        var current = this.transform;
        for (int i = 0; i < Chainlength && current != null && current.parent != null; i++)
        {
            var scale = Vector3.Distance(current.position, current.parent.position) * 0.1f;
            Handles.matrix = Matrix4x4.TRS(current.position, Quaternion.FromToRotation(Vector3.up, current.parent.position - current.position), new Vector3(scale, Vector3.Distance(current.parent.position, current.position), scale));
            Handles.color = Color.green;
            Handles.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
            current = current.parent;
        }
#endif
    }
}
