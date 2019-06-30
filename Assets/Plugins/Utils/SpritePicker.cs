using UnityEngine;

// credit goes to Daniel Koozer for his amazing solution

[ExecuteInEditMode]
public class SpritePicker : MonoBehaviour
{
  [SerializeField] Texture[] textureSheets = null;
  [SerializeField] Vector2Int frames = Vector2Int.zero;
  Transform gameCamera = null;

  Transform billboard;
  Material billboardMaterial;

  void Start()
  {
    gameCamera =  Camera.main.transform;
    billboard = transform.Find("Billboard");    
    if(Application.isPlaying)
      billboardMaterial = billboard.GetComponent<Renderer>().material;
  }

  void Update()
  {
    if(!gameCamera && Camera.main)
      gameCamera = (Transform)Camera.main.transform;
    if (!billboard || !billboardMaterial || frames == Vector2Int.zero || !gameCamera)
    {
      return;
    }

    int yaw = 0, pitch = 0;
    float theta = 0f, phi = 0f;

    SpritePick(ref theta, ref phi, ref yaw, ref pitch);
    Billboard(theta, phi);

    // set correct pitch texture
    billboardMaterial.mainTexture = textureSheets[pitch];

    // calculate material tiling
    billboardMaterial.mainTextureScale = new Vector2(1f / frames.x, 1f / frames.y);

    // and material offset
    var offsetX = (yaw % frames.x) * billboardMaterial.mainTextureScale.x;
    var offsetY = (1f - 1f / frames.y) - (yaw / frames.x) * billboardMaterial.mainTextureScale.y;
    billboardMaterial.mainTextureOffset = new Vector2(offsetX, offsetY);
  }

  void CartesianToSpherical(Vector3 v, ref float theta, ref float phi, float tolerance)
  {
    var length = new Vector2(v.z, v.x).magnitude;
    theta = length < tolerance ? 0f : Mathf.Atan2(v.x, v.z);

    var radius = new Vector2(v.y, length).magnitude;
    phi = radius < tolerance ? 0f : Mathf.Atan2(v.y, length);
  }

  Matrix4x4 DirectionVectorsToRotationMatrix(Vector3 forward, Vector3 right, Vector3 up)
  {
    var output = Matrix4x4.identity;

    output.SetColumn(0, right);   // x
    output.SetColumn(1, up);      // y
    output.SetColumn(2, forward); // z

    return output;
  }

  void SpritePick(ref float theta, ref float phi, ref int yaw, ref int pitch)
  {
    var objectToCamera = gameCamera.position - transform.position;
    objectToCamera.Normalize();

    // since the ship may be rotated, we need to take that into account before converting to spherical
    // invert the world matrix and transform our d vector to put the d vector in ship-local space
    var r = DirectionVectorsToRotationMatrix(transform.forward, transform.right, transform.up);
    objectToCamera = r.inverse.MultiplyVector(objectToCamera);

    CartesianToSpherical(objectToCamera, ref theta, ref phi, 0.01f);

    // some manipulation constants
    const float twoPi = Mathf.PI * 2f;
    const float halfPi = Mathf.PI / 2f;

    // conversion ratio between radians and sprite indices
    const float interval = Mathf.PI / 16f;
    const float halfInterval = interval / 2f;

    // keeping separate theta/phi pairs for picking and the later step of putting
    // subtracting half of an angle interval to "round" the value to the nearest sprite
    var spriteTheta = theta - halfInterval;
    var spritePhi = phi - halfInterval;

    while (spriteTheta < 0f)
    {
      spriteTheta += twoPi;
    }

    while (spriteTheta > twoPi)
    {
      spriteTheta -= twoPi;
    }

    // phi ranges from -90 (bottom) to +90 (top), so convert that range to 180..0
    spritePhi = halfPi - spritePhi;

    // 0..31
    yaw = (int)(spriteTheta / interval);

    // 0..16
    pitch = (int)(spritePhi / interval);

    // convert theta and phi back to usable values for steps 2+, notice these are snapped to nearest sprite yaw/pitch
    // adding one due to the fact that zero is not front-on
    theta = (yaw + 1) * interval;

    // phi values used by a 'rotate about X axis' transform needs to be positive = nose-down, negative = nose-up
    // this is opposite the picking and spherical coordinates
    phi = pitch * interval - halfPi;
  }

  void Billboard(float theta, float phi)
  {
    // billboard flip necessary since the sprite texture coordinates are rotated 180 degrees from what the world matrix is going to be
    var flip = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(180.0f, Vector3.up), Vector3.one);
    var pitch = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(phi * Mathf.Rad2Deg, Vector3.right), Vector3.one);
    var yaw = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(theta * Mathf.Rad2Deg, Vector3.up), Vector3.one);

    var view = yaw * pitch * flip;

    // construct billboard matrix
    var world = DirectionVectorsToRotationMatrix(transform.forward, transform.right, transform.up);
    var billboardMatrix = world * view;
    billboard.transform.rotation = Quaternion.LookRotation(billboardMatrix.GetColumn(2), billboardMatrix.GetColumn(1));
  }
}
