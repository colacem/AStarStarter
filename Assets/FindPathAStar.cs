using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PathMarker
{
    public MapLocation location;
    public float G;
    public float H;
    public float F;
    public GameObject marker;
    public PathMarker parent;

    public PathMarker(MapLocation l, float g, float h, float f, GameObject marker, PathMarker p)
    {
        location=l;
        G=g;
        H=h;
        F=f;
        this.marker=marker;
        parent=p;
    }
    public override bool Equals(object obj)
    {
        if ((obj==null)||!this.GetType().Equals(obj.GetType()))
        {
            return false;
        }   
        else
        {
            return  location.Equals(((PathMarker)obj).location);
        }
    }

    public override int GetHashCode()
    {
        return 0;
    }
}

public class FindPathAStar : MonoBehaviour
{
    public Maze maze;
    public Material closedMaterial;
    public Material openMaterial;
    List<PathMarker> open=new List<PathMarker>();
    List<PathMarker> closed=new List<PathMarker>();
    public GameObject start;
    public GameObject end;
    public GameObject pathP;
    private bool isRun;

    PathMarker goalNode;
    PathMarker startNode;
    PathMarker lastPos;
    bool done=false;

    void RemoveAllMarkers()
    {
        GameObject[] markers= GameObject.FindGameObjectsWithTag("marker");
        foreach  (GameObject m in markers)
        {
            Destroy(m);
        }
    }
    void BeginSearch()
    {
        done=false;
        isRun=true;
        RemoveAllMarkers();

        //llenamos una lista con todas las ubicaciones que no son paredes para saber luego donde podemos movernos
        List<MapLocation> locations = new List<MapLocation>();
        for (int z=1; z<maze.depth-1;z++)
            for (int x=1; x<maze.width -1;x++)
            {
                if (maze.map[x,z]!=1)
                    locations.Add(new MapLocation(x,z));
            }

        //Barajo las posiciones disponibles
        locations.Shuffle();

        //tomo la primera posicion de la lista para comenzar y la segunda para el destino
        Vector3 startLocation = new Vector3(locations[0].x * maze.scale,0,locations[0].z * maze.scale);
        startNode = new PathMarker(new MapLocation(locations[0].x, locations[0].z),0,0,0,
                    Instantiate(start,startLocation,Quaternion.identity),null);
        
        Vector3 goalLocation = new Vector3(locations[1].x * maze.scale,0,locations[1].z * maze.scale);
        goalNode = new PathMarker(new MapLocation(locations[1].x, locations[1].z),0,0,0,
                    Instantiate(end,goalLocation,Quaternion.identity),null);

        //Limpio las listas
        open.Clear();
        closed.Clear();

        //Añado la primera posicion a la lista abierta
        open.Add(startNode);
        lastPos=startNode;
    }

    void Search(PathMarker thisNode)
    {
        //Verifico si llegó al final
        if (thisNode.Equals(goalNode)) {done=true; return;}

        //Buscamos todos los vecinos para añadirlos a la lista abierta
        foreach(MapLocation dir in maze.directions)
        {
            MapLocation neighbour = dir + thisNode.location;
            //Verifico que la posicion vecino no sea un muro
            if (maze.map[neighbour.x, neighbour.z]==1) continue;
            //Verifico que no se salga del laberinto
            if (neighbour.x < 1 || neighbour.x >= maze.width || neighbour.z < 0 || neighbour.z >= maze.depth) continue;
            //Verifico que tampoco esté en la lista cerrada
            if (IsCosed(neighbour)) continue;

            //Calculo la distancia desde el inicio de ruta
            float G = Vector2.Distance(thisNode.location.ToVector(),neighbour.ToVector()) + thisNode.G;
            //Calculo la distancia del vecino hasta la posicion final
            float H = Vector2.Distance(neighbour.ToVector(),goalNode.location.ToVector());
            float F = G + H; //Camino Total

            GameObject pathBlock = Instantiate(pathP, new Vector3(neighbour.x * maze.scale,0, neighbour.z * maze.scale),
                                    Quaternion.identity);
            //Añado los valores a unos textmesh que están en el prefab para verlos
            TextMesh[] values = pathBlock.GetComponentsInChildren<TextMesh>();
            values[0].text ="G: " + G.ToString("0.00");
            values[1].text ="H: " + H.ToString("0.00");
            values[2].text ="F: " + F.ToString("0.00");

            //Si está el nodo en la lista abierta lo actualizo, sino lo añado como nuevo
            if (!UpdateMarker(neighbour,G,H,F,thisNode))
                open.Add(new PathMarker(neighbour,G,H,F,pathBlock,thisNode));
        }

        //Ordenamos los vecinos para saber cual es que tiene el F mas bajo y seguir camino
        open = open.OrderBy(p => p.F).ToList<PathMarker>();
        //Seleccionamos el primero que será el de la F mas pequeña
        PathMarker pm = (PathMarker) open.ElementAt(0);
        //Lo añadimos a la lista cerrada
        closed.Add(pm);
        //Lo quitamos de la lista abierta
        open.RemoveAt(0);
        //Le cambiamos el material para que sea de color closed
        pm.marker.GetComponent<Renderer>().material = closedMaterial;
        //Actualizamos la ultima posicion
        lastPos=pm;

    }
    bool UpdateMarker(MapLocation pos, float g, float h, float f, PathMarker prt)
    {
        foreach (PathMarker p in open)
        {
            if (p.location.Equals(pos))
            {
                p.G=g;
                p.H=h;
                p.F=f;
                p.parent=prt;
                return true;
            }
        }
        return false;
    }
    bool IsCosed(MapLocation marker)
    {
        foreach(PathMarker p in closed) 
        {
            if (p.location.Equals(marker)) return true;   
        }
        return false;
    }

    void GetPath()
    {   
        RemoveAllMarkers();
        PathMarker begin = lastPos;
        //Dibujamos el mapa siguiendo desde la ultima posicion los parent guardados.
        while (!startNode.Equals(begin) && begin != null)
        {
            Instantiate(pathP,new Vector3(begin.location.x * maze.scale,0,begin.location.z * maze.scale),
                        Quaternion.identity);
            begin = begin.parent;
        }
        Instantiate(pathP,new Vector3(startNode .location.x * maze.scale,0,startNode.location.z * maze.scale),
                        Quaternion.identity);

    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) BeginSearch();
        if (Input.GetKeyDown(KeyCode.C) && !done && isRun) Search(lastPos);
        if (Input.GetKeyDown(KeyCode.M) && done) GetPath();
    }
}
