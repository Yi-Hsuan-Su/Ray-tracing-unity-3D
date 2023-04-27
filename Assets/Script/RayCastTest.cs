using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Threading.Tasks;
using Chapter10;

public interface IMaterial
{
bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered);
};
public struct Hit_record
{
    public RaycastHit hit;
    public IMaterial mat;
}

public class Lambertian : IMaterial
{
    public Vector3 albedo;
    public float reflect;

    public Lambertian(Vector3 a, float r)
    {
        albedo = a;
        reflect = r;
    }

    public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered)
    {
        Vector3 target = rec.hit.normal.normalized +
            new Vector3(Random.Range(-1, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

        scattered.origin = rec.hit.point;
        scattered.direction = target;
        attenuation = albedo * reflect;
        return true;
    }
}


public class MetalNoFuzz : IMaterial
{
    public Vector3 albedo;

    public MetalNoFuzz(Vector3 a)
    {
        albedo = a;
    }

    public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered)
    {
        Vector3 reflected = Vector3.Reflect(r.direction.normalized, rec.hit.normal.normalized);

        scattered.origin = rec.hit.point;
        scattered.direction = reflected;
        attenuation = albedo;
        return Vector3.Dot(scattered.direction, rec.hit.normal) > 0;
    }
}

public class Metal : IMaterial
{
    public Vector3 albedo;
    public float fuzz;

    public Metal(Vector3 a, float f)
    {
        albedo = a;
        fuzz = f;
    }

    private Vector3 Random_in_unit_sphere()
    {
        return new Vector3(Random.Range(-1f, 1f), Random.Range(-1, 1f), Random.Range(-1f, 1f)).normalized;
    }

    public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered)
    {
        Vector3 reflected = Vector3.Reflect(r.direction.normalized, rec.hit.normal.normalized);
        reflected = reflected + fuzz * Random_in_unit_sphere();

        scattered.origin = rec.hit.point;
        scattered.direction = reflected;
        attenuation = albedo;
        return Vector3.Dot(scattered.direction, rec.hit.normal) > 0;
    }
}

public class RayCastTest : MonoBehaviour
{
    public GameObject MyLight;
    public string SavedImgName;
    public int SampleNum = 4;
    public int MaxDepth = 2;
    public int width;
    public int height;
    public static int sampleNum = 16;
    public static int maxDepth = 2;

    private static Vector3 topColor = Vector3.one;
    private static Vector3 bottomColor = new Vector3(0.5f, 0.7f, 1.0f);
    private static Vector3 ballColor = new Vector3(1, 0, 0);
    static int nx = 10;
    static int ny = 10;
    static Camera cam;
    static Texture2D tex;
    // Start is called before the first frame update
    void Start()
    {
        sampleNum = SampleNum;
        maxDepth = MaxDepth;
        nx = width;
        ny = height;

        cam = this.GetComponent<Camera>();
        //cam.aspect = 1;
        Debug.Log("Start");
        Vector3 ori_color = new Vector3(0, 0, 0);
        tex = ImageHelper.CreateImg(nx, ny);

        int minxnx = 400 - nx / 2;
        int minxny = 400 - ny / 2;


        for (int j = (ny - 1)/2 + 400; j >= minxny; --j)
        {
            for (int i = minxnx; i < (nx/2+400); ++i)
            {
                Vector3 color = Vector3.zero;
                for (int k = 0; k < sampleNum; ++k)
                {
                    //MyLight.GetComponent<Col>
                    float u = ((float)(i) + Random.Range(-1f, 1f));
                    float v = ((float)(j) + Random.Range(-1f, 1f));
                    Vector3 pos = new Vector3(u, v, 0);
                    Ray r = cam.ScreenPointToRay(pos);
                    color += RayCast(r, ori_color, 0);
                    // Ray r = new Ray(origin, lower_left_corner + u * horizontal + v * vertical);
                }
                color = color / (float)(sampleNum / 4);
                color.x = Mathf.Sqrt(color.x);
                color.y = Mathf.Sqrt(color.y);
                color.z = Mathf.Sqrt(color.z);

                ImageHelper.SetPixel(tex, i, j, color);

            }
        }

        ImageHelper.SaveImg(tex, "Img/" + SavedImgName +".png");
        Debug.Log("Finish");
        
    }

    private static Vector3 GetTextureColor(RaycastHit hit)
    {
        if(hit.transform.gameObject.tag != "Light")
        {
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            MeshCollider meshCollider = hit.collider as MeshCollider;
         //   Debug.Log("Obj name is " + hit.transform.gameObject.name);
            int triangleIdx = hit.triangleIndex;
            Mesh mesh = hit.collider.gameObject.GetComponent<MeshFilter>().mesh;
            int subMeshesNr = mesh.subMeshCount;
            int materialIdx = -1;
            for (int i = 0; i < subMeshesNr; i++)
            {
                var tr = mesh.GetTriangles(i);
                for (var j = 0; j < tr.Length; j++)
                {
                    if (tr[j] == triangleIdx)
                    {
                        materialIdx = i;
                        break;
                    }
                }
                if (materialIdx != -1) break;
            }
            if (materialIdx != -1)
            {
              //  Debug.Log("-------------------- I'm using " + renderer.materials[materialIdx].name + " material(s)");
                // Texture mainTexture = renderer.materials[materialIdx].mainTexture;
                // Texture2D texture2D = new Texture2D(mainTexture.width, mainTexture.height, TextureFormat.RGBA32, false);
                Texture2D texture2D = renderer.materials[materialIdx].mainTexture as Texture2D;
                if (texture2D != null)
                {
                    Vector2 pCoord = hit.textureCoord;
                    pCoord.x *= texture2D.width;
                    pCoord.y *= texture2D.height;

                    Vector2 tiling = renderer.material.mainTextureScale;
                    Color color = texture2D.GetPixel(Mathf.FloorToInt(pCoord.x * tiling.x), Mathf.FloorToInt(pCoord.y * tiling.y));
                    return new Vector3(color.r* renderer.material.color.r, color.g * renderer.material.color.g, color.b * renderer.material.color.b);

                }
                return new Vector3(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b); ;

            }
            return new Vector3(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b); ;
        }
        return new Vector3(0, 0, 0);
        // Debug.Log("-------------------- I'm using " + renderer.materials[materialIdx].name + " material(s)");
    }
    public static Vector3 RayCast(Ray ray, Vector3 color, int depth, Hit_record rec = new Hit_record())
    {
        Vector3 unit_direction;
        float t = 0;
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000))
        {
            rec.hit = hit;
            Ray scattered = new Ray();
            Vector3 attenuation = Vector3.one;
            if (depth == 0)
            {
                color = GetTextureColor(hit);

                if (hit.transform.tag == "Lambertian")
                {
                    rec.mat = new Lambertian(color, 0.382f);
                }
                else if (hit.transform.tag == "MetalNoFuzz")
                {
                    rec.mat = new MetalNoFuzz(color);

                }
                else if (hit.transform.tag == "Metal")
                {
                    rec.mat = new Metal(color, 0.6f);
                }
                // Debug.Log("Color " + color);
            }

           
 
            if (hit.transform.tag == "Light" || depth > maxDepth)
            {
                unit_direction = ray.direction.normalized;
                if (hit.transform.tag == "Light" && depth == 0)
                    return new Vector3(1, 1, 1);
                t = 0.3f * depth;
                return Vector3.Lerp(color, new Vector3(0, 0, 0), t);
            }
            else if(depth <= maxDepth && rec.mat.Scatter(ref ray, ref rec, ref attenuation, ref scattered))
            {
                var tmpC = RayCast(scattered, color, depth + 1, rec);
                attenuation.x *= tmpC.z;
                attenuation.y *= tmpC.y;
                attenuation.z *= tmpC.z;
                return attenuation;
            }
            else
            {
                return Vector3.zero;
            }

            
          /*  else
            {
                var target = hit.normal.normalized +
new Vector3(Random.Range(-1, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

                return 0.5f * RayCast(new Ray(hit.point, target), color, depth + 1);
            }*/
        }

        unit_direction = ray.direction.normalized;
        t = 0.5f * (unit_direction.y + 1.0f);
        return Vector3.Lerp(topColor, bottomColor, t);

    }

    // Update is called once per frame
   /* void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            /* Debug.Log("RayCast!");
             Ray ray = new Ray(transform.position, transform.forward * 100);
             Debug.DrawLine(transform.position, transform.position + transform.forward * 100, Color.red);

             RaycastHit hit;
             if (Physics.Raycast(ray, out hit, 10))
             {
                 Debug.Log(hit.point);
                 Debug.Log(hit.transform.position);
                 Debug.Log(hit.collider.gameObject);
             }
        }
    }*/
}
