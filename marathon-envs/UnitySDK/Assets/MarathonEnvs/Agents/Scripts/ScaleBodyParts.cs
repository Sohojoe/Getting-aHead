using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ScaleBodyParts : MonoBehaviour, IOnHandleModelReset
{

    public Collider[] LimbsToScale;

    public float[] LimbScale;

    float[] _capsuleHeights;
    //float[] _capsuleRadius;
    float[] _yPositions;
    float[] _masses;

    public void OnHandleModelReset()
    {
        Scale();
    }

    private void Awake()
    {
        // get default params
        _capsuleHeights = LimbsToScale.Select(x => {
            CapsuleCollider capsuleCollider = x as CapsuleCollider;
            if (x == null)
                return 0f;
            return capsuleCollider.height;
        }).ToArray();
        //_capsuleRadius = LimbsToScale.Select(x => {
        //    CapsuleCollider capsuleCollider = x as CapsuleCollider;
        //    if (x == null)
        //        return 0f;
        //    return capsuleCollider.radius;
        //}).ToArray();
        _yPositions = LimbsToScale.Select(x => x.transform.position.y).ToArray();
        _masses = LimbsToScale.Select(x => x.GetComponent<Rigidbody>().mass).ToArray();

    }

    void Scale()
    {
        for (var i=0; i<LimbsToScale.Length; i++)
        {
            var limbCollider = LimbsToScale[i];
            var obj = limbCollider.gameObject;
            Rigidbody rigidbody = obj.GetComponent<Rigidbody>();
            ConfigurableJoint configurableJoint = obj.GetComponent<ConfigurableJoint>();
            ProceduralCapsule proceduralCapsule = obj.GetComponent<ProceduralCapsule>();
            var scale = LimbScale[i];

            float height = _capsuleHeights[i] * scale;
            float yOffset = (_capsuleHeights[i] - height) / 2;
            float mass = _masses[i] * scale;
            CapsuleCollider capsuleCollider = limbCollider as CapsuleCollider;
            if (limbCollider != null)
            {
                capsuleCollider.height = height;
                rigidbody.mass = mass;
                Vector3 v = ColliderDirectionToVector(capsuleCollider.direction);
                obj.transform.position += v * yOffset;
                if (configurableJoint != null)
                    configurableJoint.anchor += v * -yOffset;
                var children = obj.GetComponentsInChildren<Transform>()
                    .Where(x => x.parent == obj.transform);
                foreach (var childTransform in children)
                {
                    childTransform.position = new Vector3(
                        childTransform.position.x,
                        childTransform.position.y + yOffset,
                        childTransform.position.z);

                }
                if (proceduralCapsule != null)
                {
                    proceduralCapsule.height = capsuleCollider.height;
                    proceduralCapsule.radius = capsuleCollider.radius;
                    proceduralCapsule.CreateMesh();
                }

            }
        }
    }
    Vector3 ColliderDirectionToVector(int direction)
    {
        switch (direction)
        {
            case 0:
                return new Vector3(1f, 0, 0);
            case 1:
                return new Vector3(0, 1f, 0);
            case 2:
                return new Vector3(0, 0, 1f);
            default:
                throw new System.ArgumentException();
        }
    }
}