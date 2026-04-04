using UnityEngine;
using System.Collections;

public class Produccion : MonoBehaviour
{
    public Spawner spawnBase;
    public Spawner spawnPCB;
    public Spawner spawnMotor1;
    public Spawner spawnMotor2;
    public Spawner spawnMotor3;
    public Spawner spawnMotor4;
    public Spawner spawnHelice1;
    public Spawner spawnHelice2;
    public Spawner spawnHelice3;
    public Spawner spawnHelice4;
    public Spawner spawnTapa;

    [Header("Cajas (una por punto de spawn)")]
    public Spawner[] spawnsCaja; // arrastra aquí SpawnCaja(1), (2) y (3)

    void Start()
    {
        StartCoroutine(SecuenciaEnsamblaje());
    }

    IEnumerator SecuenciaEnsamblaje()
    {
        spawnBase.Spawn();
        yield return new WaitForSeconds(2);

        spawnPCB.Spawn();
        yield return new WaitForSeconds(2);

        spawnMotor1.Spawn();
        spawnMotor2.Spawn();
        yield return new WaitForSeconds(2);

        spawnMotor3.Spawn();
        spawnMotor4.Spawn();
        yield return new WaitForSeconds(2);

        spawnHelice1.Spawn();
        spawnHelice2.Spawn();
        yield return new WaitForSeconds(2);

        spawnHelice3.Spawn();
        spawnHelice4.Spawn();
        yield return new WaitForSeconds(2);

        spawnTapa.Spawn();
        yield return new WaitForSeconds(2);

        // Todas las cajas al mismo tiempo
        foreach (Spawner sc in spawnsCaja)
            sc.Spawn();
    }
}