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
    public Spawner spawnCaja;

    void Start()
    {
        StartCoroutine(SecuenciaEnsamblaje());
    }

    IEnumerator SecuenciaEnsamblaje()
    {
        // BASE
        spawnBase.Spawn();
        yield return new WaitForSeconds(2);

        // PCB
        spawnPCB.Spawn();
        yield return new WaitForSeconds(2);

        // MOTORES 1 y 2
        spawnMotor1.Spawn();
        spawnMotor2.Spawn();
        yield return new WaitForSeconds(2);

        // MOTORES 3 y 4
        spawnMotor3.Spawn();
        spawnMotor4.Spawn();
        yield return new WaitForSeconds(2);

        // HELICES 1 y 2
        spawnHelice1.Spawn();
        spawnHelice2.Spawn();
        yield return new WaitForSeconds(20);

        // HELICES 3 y 4
        spawnHelice3.Spawn();
        spawnHelice4.Spawn();
        yield return new WaitForSeconds(20);

        // TAPA
        spawnTapa.Spawn();
        yield return new WaitForSeconds(100);

        // CAJA
        spawnCaja.Spawn();
    }
}