using UnityEngine;

public class Angulos : MonoBehaviour
{
    [Header("Articulaciones principales")]
    public ArticulationBody Waist;              // X Drive
    public ArticulationBody Arm01;              // Z Drive
    public ArticulationBody Arm02;              // Z Drive
    public ArticulationBody Arm03;              // X Drive
    public ArticulationBody GripperAssembly;    // Z Drive
    public ArticulationBody Gear1;              // X Drive
    public ArticulationBody Gear2;              // X Drive

    [Header("Ángulos que controlas por código")]
    public float waistTarget = 0f;
    public float arm01Target = 0f;
    public float arm02Target = 0f;
    public float arm03Target = 0f;
    public float gripAssemblyTarget = 0f;

    [Header("Control del gripper")]
    public float gripperOpenAngle = 15f;
    public float gripperClosedAngle = -20f;
    public bool gripperClosed = false;

    [Header("Velocidad de movimiento (°/s)")]
    public float speed = 60f;

    void Update()
    {
        // Mueve cada articulación hacia su ángulo objetivo
        SmoothX(Waist, waistTarget);
        SmoothZ(Arm01, arm01Target);
        SmoothZ(Arm02, arm02Target);
        SmoothX(Arm03, arm03Target);
        SmoothZ(GripperAssembly, gripAssemblyTarget);

        // Control de engranajes del gripper
        float targetGrip = gripperClosed ? gripperClosedAngle : gripperOpenAngle;
        SmoothX(Gear1, targetGrip);
        SmoothX(Gear2, -targetGrip);
    }

    // === MÉTODOS AUXILIARES ===

    void SmoothX(ArticulationBody joint, float target)
    {
        if (joint == null) return;
        var drive = joint.xDrive;
        float step = speed * Time.deltaTime;
        drive.target = Mathf.MoveTowards(drive.target, target, step);
        joint.xDrive = drive;
    }

    void SmoothZ(ArticulationBody joint, float target)
    {
        if (joint == null) return;
        var drive = joint.zDrive;
        float step = speed * Time.deltaTime;
        drive.target = Mathf.MoveTowards(drive.target, target, step);
        joint.zDrive = drive;
    }
}
