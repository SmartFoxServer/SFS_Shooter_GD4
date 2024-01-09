using Godot;
using System;

using Sfs2X.Entities.Data;
/**
* The Character Transform is created to transmit to the server code.
* The position, rotation and spine rotation are placed in an SFS Object and transminted using UDP Protocol
* This is uitilised for both send and receive.
*/
public class CharacterTransform
{
    private Vector3 position;
    private Vector3 angleRotation;
    private Vector3 spineRotation;
    private double timeStamp = 0;

    #region  Transform Calls
    public Vector3 Position
    {
        get
        {
            return position;
        }
    }
    public Vector3 AngleRotation
    {
        get
        {
            return angleRotation;
        }
    }
    public Vector3 SpineRotation
    {
        get
        {
            return spineRotation;
        }
    }
    public Vector3 AngleRotationFPS
    {
        get
        {
            return new Vector3(angleRotation.X, angleRotation.Y, angleRotation.Z);
        }
    }
    public Quaternion Rotation
    {
        get
        {
            return new Quaternion(AngleRotationFPS.X, AngleRotationFPS.Y, AngleRotationFPS.Z, 1.0f);
        }
    }
    public Vector3 SpineRotationFPS
    {
        get
        {
            return new Vector3(spineRotation.X, spineRotation.Y, spineRotation.Z);
        }
    }
    public Quaternion RotationSpine
    {
        get
        {
            return new Quaternion(SpineRotationFPS.X, SpineRotationFPS.Y, SpineRotationFPS.Z, 1.0f);
        }
    }
    #endregion
    
    /**
    * The Timestamp is added to the Transform Object for Interpolation
    */

    #region  TimeStamp Method
    public double TimeStamp
    {
        get
        {
            return timeStamp;
        }
        set
        {
            timeStamp = value;
        }
    }
    #endregion
    
    /**
    * Add the Characters position, rotation and spoine rotation to an SFS Object
    */

    #region  Add Transform to SFSObject
    public void ToSFSObject(ISFSObject data)
    {
        ISFSObject tr = new SFSObject();
        tr.PutDouble("x", Convert.ToDouble(this.position.X));
        tr.PutDouble("y", Convert.ToDouble(this.position.Y));
        tr.PutDouble("z", Convert.ToDouble(this.position.Z));
        tr.PutDouble("rx", Convert.ToDouble(this.angleRotation.X));
        tr.PutDouble("ry", Convert.ToDouble(this.angleRotation.Y));
        tr.PutDouble("rz", Convert.ToDouble(this.angleRotation.Z));
        tr.PutDouble("srx", Convert.ToDouble(this.spineRotation.X));
        tr.PutDouble("sry", Convert.ToDouble(this.spineRotation.Y));
        tr.PutDouble("srz", Convert.ToDouble(this.spineRotation.Z));
        tr.PutLong("t", Convert.ToInt64(this.timeStamp));
        data.PutSFSObject("transform", tr);
    }

    public void Load(CharacterTransform chtransform)
    {
        this.position = chtransform.position;
        this.angleRotation = chtransform.angleRotation;
        this.spineRotation = chtransform.spineRotation;
        this.timeStamp = chtransform.timeStamp;
    }
    #endregion
    
    /**
    * Extract the Characters position, rotation and spoine rotation from the SFS Object
    */

    #region  Extract Transform to SFSObject
    public static CharacterTransform FromSFSObject(ISFSObject data)
    {
        CharacterTransform chtransform = new CharacterTransform();
        ISFSObject transformData = data.GetSFSObject("transform");
        float x = Convert.ToSingle(transformData.GetDouble("x"));
        float y = Convert.ToSingle(transformData.GetDouble("y"));
        float z = Convert.ToSingle(transformData.GetDouble("z"));
        float rx = Convert.ToSingle(transformData.GetDouble("rx"));
        float ry = Convert.ToSingle(transformData.GetDouble("ry"));
        float rz = Convert.ToSingle(transformData.GetDouble("rz"));
        float srx = Convert.ToSingle(transformData.GetDouble("srx"));
        float sry = Convert.ToSingle(transformData.GetDouble("sry"));
        float srz = Convert.ToSingle(transformData.GetDouble("srz"));
        chtransform.position = new Vector3(x, y, z);
        chtransform.angleRotation = new Vector3(rx, ry, rz);
        chtransform.spineRotation = new Vector3(srx, sry, srz);
        if (transformData.ContainsKey("t"))
        {
            chtransform.TimeStamp = Convert.ToDouble(transformData.GetLong("t"));
        }
        else
        {
            chtransform.TimeStamp = 0;
        }
        return chtransform;
    }

    public static CharacterTransform FromTransform(Transform3D transform, Transform3D transVisual, Vector3 transAim)
    {
        CharacterTransform trans = new CharacterTransform();
        trans.position = transform.Origin;
        trans.angleRotation = transVisual.Basis.GetEuler();
        trans.spineRotation = transAim;

        return trans;
    }
    public void ResetTransform(Transform3D trans)
    {
        trans.Origin = this.Position;
        trans.LookingAt(this.AngleRotation, new Vector3(0, 0, 1));
    }
#endregion
}
