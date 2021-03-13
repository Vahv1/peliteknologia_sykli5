using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Make your generation algorithm here
public class Generate : MonoBehaviour
{

    public TileType[] tileTypes; // all the tiletypes given in the editor
    public GameObject player;
    public GameObject enemy;

    //the size of the generated map
    private int mapSizeX = 10;
    private int mapSizeY = 10;

    private float levelHalfLength; //Tähän talteen puolet kentän pituudesta vinosuunnassa
    private Vector3 playerPosition;
    private Vector3 endPosition;
    private List<Vector3> grassPositions;

    void Start()
    {
        // Jotenkin näin se matikka ja pythagoras toimii 
        levelHalfLength = (float)(System.Math.Sqrt(mapSizeX * mapSizeX + mapSizeY * mapSizeY) / 2) - 1;
        GenerateMap();
    }

    // Generoi kentän
    void GenerateMap()
    {
        // Spawnataan pelaaja ja loppuruutu sekä otetaan niiden sijainnit talteen
        playerPosition = SpawnPlayer();
        endPosition = SetEnding(playerPosition);

        // Luodaan polku joka menee lähdöstä loppuun ja täytetään se ruohotileillä, varmistaa aina ratkaistavuuden
        grassPositions = GeneratePath(playerPosition, endPosition);
        foreach (Vector3 pos in grassPositions)
        {
            Instantiate(tileTypes[0].tileVisual, pos, Quaternion.identity);
        }

        // Asetellaan loput tilet polun lisäksi
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                // Ei aseteta paikkoihin, joissa on jo ruohopolku
                if (!grassPositions.Contains(new Vector3(x, y, -0.5f)))
                {
                    TileType tt;

                    /* Asetetaan n. puolet lopuista ruuduista ruohoksi ja puolet seiniksi.
                     * Näin ruohoja tulee enemmän kuin seiniä, mutta tuli tosi tylsiä kenttiä, jos laittoi koko kentän 50/50 suhteella.
                     * Sitten olisi tarvinnut jonkun kehittyneemmän generaattorin ettei tulisi kenttä täyteen ruohoalueita, joille ei voi päästä.
                     */
                    int rnd = Random.Range(0, 2);
                    if (rnd == 0)
                    {
                        tt = tileTypes[0];
                        //Lisätään ruoholistaan niin on ajantasainen lista kaikista paikoista joissa on ruohoa
                        grassPositions.Add(new Vector3(x, y, -0.5f));
                    }
                    else tt = tileTypes[1];

                    Instantiate(tt.tileVisual, new Vector3(x, y, -0.5f), Quaternion.identity);
                }
            }
        }

        // Spawnataan 1-3 vihua
        int enemyAmount = Random.Range(1, 4);
        for (int i = 0; i < enemyAmount; i++)
        {
            SpawnEnemy();
        }
    }

    // Spawnaa pelaajan ja aloituspaikan randomsijaintiin kentällä 
    Vector3 SpawnPlayer()
    {
        int spawnX = Random.Range(0, mapSizeX);
        int spawnY = Random.Range(0, mapSizeY);
        Vector3 spawnPoint = new Vector3(spawnX, spawnY, -0.5f);
        Instantiate(tileTypes[2].tileVisual, spawnPoint, Quaternion.identity);
        Instantiate(player, spawnPoint + new Vector3(0, 0, -0.1f), Quaternion.identity);
        return spawnPoint;
    }

    // Spawnaa loppupisteen sopivan matkan päähän pelaajasta
    Vector3 SetEnding(Vector3 playerPosition)
    {
        List<Vector3> validEndPositions = new List<Vector3>();

        // Asetetaan listaan kaikki pisteet, jotka on tarpeeksi kaukana pelaajasta
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                Vector3 pos = new Vector3(x, y, -0.5f);
                if (Vector3.Distance(pos, playerPosition) > levelHalfLength)
                {
                    validEndPositions.Add(pos);
                }
            }
        }

        // Valitaan listasta randomilla loputusruudun paikka
        Vector3 spawnPoint = validEndPositions[Random.Range(0, validEndPositions.Count)];
        Instantiate(tileTypes[3].tileVisual, spawnPoint, Quaternion.identity);
        return spawnPoint;
    }

    // Spawnaa vihollisen seinän taakse pelaajasta
    void SpawnEnemy()
    {
        List<Vector3> validEnemyPositions = new List<Vector3>();
        // Lisätään listaan kaikki ruohosijainnit, jotka on seinän takana pelaajaan nähden
        foreach (Vector3 pos in grassPositions)
        {
            if (IsBehindWall(playerPosition, pos))
            {
                validEnemyPositions.Add(pos);
            }
        }
        // Valitaan listasta randomilla vihollisen paikka
        Vector3 spawnPoint = validEnemyPositions[Random.Range(0, validEnemyPositions.Count)];
        Instantiate(enemy, spawnPoint + new Vector3(0, 0, -0.1f), Quaternion.identity);
    }

    // Tarkistaa onko endPoint seinän takana startPointista
    /* Toimii tekemällä lyhimmän suorakulmaisen polun alkupisteestä lähtöpisteeseen
     * molempia eri reittejä ja tarkistamalla oliko kummallakaan polulla seiniä.
     * Jos jommallakummalla polulla oli seiniä, on sijainti seinän takana.
     * Aika huono logiikka näin jälkikäteen tarkasteltuna, mutta en oo ihan varma, mitkä kaikki
     * lasketaan seinän takana olevaksi ja tein jo tän niin mennään nyt sillä */
    bool IsBehindWall(Vector3 startPoint, Vector3 endPoint)
    {
        List<Vector3> pathToEnd = new List<Vector3>();
        Vector3 currentPoint = startPoint;
        int xAdd = 0;
        int yAdd = 0;

        // Asetetaan liikkumissuunta sen mukaan, missä loppupiste on
        if (endPoint.x > startPoint.x) xAdd = 1;
        else if (endPoint.x < startPoint.x) xAdd = -1;

        if (endPoint.y > startPoint.y) yAdd = 1;
        else if (endPoint.y < startPoint.y) yAdd = -1;

        // Liikutaan ensin x-akselia loppupisteen kohdalle, lisätään listaan matkalla olleet sijainnit
        while (currentPoint.x != endPoint.x)
        {
            currentPoint = currentPoint + new Vector3(xAdd, 0, 0);
            pathToEnd.Add(currentPoint);
        }
        // Sen jälkeen liikutaan y-akselia loppupisteen kohdalle, lisätään listaan matkalla olleet sijainnit
        while (currentPoint.y != endPoint.y)
        {
            currentPoint = currentPoint + new Vector3(0, yAdd, 0);
            pathToEnd.Add(currentPoint);
        }

        //Aloitetaan taas lähtöpisteestä
        currentPoint = startPoint;
        // Liikutaan ensin y-akselia loppupisteen kohdalle, lisätään listaan matkalla olleet sijainnit
        while (currentPoint.y != endPoint.y)
        {
            currentPoint = currentPoint + new Vector3(0, yAdd, 0);
            pathToEnd.Add(currentPoint);
        }
        // Sen jälkeen liikutaan x-akselia loppupisteen kohdalle, lisätään listaan matkalla olleet sijainnit
        while (currentPoint.x != endPoint.x)
        {
            currentPoint = currentPoint + new Vector3(xAdd, 0, 0);
            pathToEnd.Add(currentPoint);
        }

        // Käydään kaikki poluilla olleet ruudut lävitse ja jos joku niistä ei ole ruoholistassa
        // niin palautetaan true, koska silloin se on seinä
        foreach (Vector3 pos in pathToEnd)
        {
            if (!grassPositions.Contains(pos)) return true;
        }

        // Jos kaikki polun palat oli ruoholistassa, sijainti ei ole seinän takana, palautetaan false
        return false;
    }

    // Generoidaan polku lähdöstä maaliin.
    // Liikutaan aina randomilla joku x- tai y-akselilla maalin suuntaan.
    List<Vector3> GeneratePath(Vector3 startPoint, Vector3 endPoint)
    {
        Vector3 currentPoint = startPoint;
        List<Vector3> nextPositions;
        List<Vector3> pathPoints = new List<Vector3>();

        while (currentPoint != endPoint)
        {
            // Haetaan mahdolliset seuraavat sijainnit joihin liikkua
            nextPositions = GetNextPathPosition(currentPoint);
            // Valitaan randomilla yksi sijainneista
            Vector3 rndNextPos = nextPositions[Random.Range(0, nextPositions.Count)];
            currentPoint = rndNextPos;
            pathPoints.Add(rndNextPos);
        }

        return pathPoints;
    }

    // Antaa listan mahdollisista sijainneista, jotka on yhden ruudun lähempänä endPositionia
    List<Vector3> GetNextPathPosition(Vector3 point)
    {
        int xAdd = 0;
        int yAdd = 0;
        List<Vector3> nextPositionList = new List<Vector3>();

        // Asetetaan askeleen suunta sen mukaan, missä loppupiste on
        if (endPosition.x > point.x) xAdd = 1;
        else if (endPosition.x < point.x) xAdd = -1;

        if (endPosition.y > point.y) yAdd = 1;
        else if (endPosition.y < point.y) yAdd = -1;

        // Jos ei olla x-akselilla vielä endPositionin kohdalla, voidaan liikkua yksi askel x-akselilla
        if (point.x != endPosition.x)
        {
            Vector3 newPoint = point + new Vector3(xAdd, 0, 0);
            nextPositionList.Add(newPoint);
        }

        // Jos ei olla y-akselilla vielä endPositionin kohdalla, voidaan liikkua yksi askel y-akselilla
        if (point.y != endPosition.y)
        {
            Vector3 newPoint = point + new Vector3(0, yAdd, 0);
            nextPositionList.Add(newPoint);
        }

        return nextPositionList;
    }
}
