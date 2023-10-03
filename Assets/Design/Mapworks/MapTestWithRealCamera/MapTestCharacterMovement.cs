using UnityEngine;
using UnityEngine.InputSystem;

public class MapTestCharacterMovement : MonoBehaviour
{
	public GameObject camera;
	public float speed = 2.9f;

	private void Update()
	{
		var moveVector = new Vector3(0f,0f,0f);
		
		if (Keyboard.current.rightArrowKey.isPressed)
		{
			moveVector += new Vector3(speed * Time.deltaTime, 0f, 0f);
		}

		if (Keyboard.current.leftArrowKey.isPressed)
		{
			moveVector -= new Vector3(speed * Time.deltaTime, 0f, 0f);
		}
		
		if (Keyboard.current.upArrowKey.isPressed)
		{
			moveVector += new Vector3(0f, 0f, speed * Time.deltaTime);
		}
		
		if (Keyboard.current.downArrowKey.isPressed)
		{
			moveVector -= new Vector3(0f, 0f, speed * Time.deltaTime);
		}
		
		// We apply rotation to movement to compensate for the rotated camera
		moveVector = Quaternion.AngleAxis(camera.transform.localEulerAngles.y, Vector3.up) * moveVector;
		
		transform.position += moveVector;
		transform.rotation = Quaternion.LookRotation(moveVector);
	}
}
