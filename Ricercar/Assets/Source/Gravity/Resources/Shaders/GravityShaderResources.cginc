#ifndef GRAVITY_RESOURCES
#define GRAVITY_RESOURCES

#define DEGREES_TO_RADIANS 0.0174533

int when_eq(float x, float y) 
{
    return 1 - abs(sign(x - y));
}

int when_neq(float x, float y) 
{
    return abs(sign(x - y));
}

int when_gt(float x, float y) 
{
  return max(sign(x - y), 0);
}

int when_lt(float x, float y) 
{
    return max(sign(y - x), 0);
}

int when_ge(float x, float y) 
{
  return 1 - when_lt(x, y);
}

int when_le(float x, float y) 
{
  return 1 - when_gt(x, y);
}

int and(int a, int b) 
{
  return a * b;
}

int or(int a, int b) 
{
  return min(a + b, 1);
}

int xor(int a, int b) 
{
  return (a + b) % 2;
}

int not(int a) 
{
  return 1 - a;
}



float2x2 GetRotationMatrix(float rotation)
{
    float cosRot = cos(rotation);
    float sinRot = sin(rotation);
    float2x2 rotationMatrix = { cosRot, -sinRot, sinRot, cosRot };

    return rotationMatrix;
}

float2 Rotate(float2 vec, float rotation)
{
    float cosRot = cos(rotation);
    float sinRot = sin(rotation);
    float2x2 rotationMatrix = { cosRot, -sinRot, sinRot, cosRot };

    return mul(rotationMatrix, vec);
}

float2 Transform(float2 uv, float2 pivot, float rotation, float scale)
{
    uv -= pivot;
    uv = Rotate(uv, rotation);
    uv /= scale;
    uv += pivot;

    return uv;
}
		
float2 Transform(float2 uv, float2 pivot, float2x2 rotation, float scale)
{
    uv -= pivot;
    uv = mul(rotation, uv);
    uv /= scale;
    uv += pivot;

    return uv;
}
		
float InvLerp(float from, float to, float value) 
{
	return (value - from) / (to - from);
}

#endif // GRAVITY_RESOURCES