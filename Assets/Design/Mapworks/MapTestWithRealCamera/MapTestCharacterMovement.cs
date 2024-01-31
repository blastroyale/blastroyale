using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class MapTestCharacterMovement : MonoBehaviour
{
	public GameObject _adventureCamera;
	public float _speed = 2.9f;

	private void Update()
	{
		var moveVector = new Vector3(0f,0f,0f);
		
		if (Keyboard.current.rightArrowKey.isPressed)
		{
			moveVector += new Vector3(_speed * Time.deltaTime, 0f, 0f);
		}

		if (Keyboard.current.leftArrowKey.isPressed)
		{
			moveVector -= new Vector3(_speed * Time.deltaTime, 0f, 0f);
		}
		
		if (Keyboard.current.upArrowKey.isPressed)
		{
			moveVector += new Vector3(0f, 0f, _speed * Time.deltaTime);
		}
		
		if (Keyboard.current.downArrowKey.isPressed)
		{
			moveVector -= new Vector3(0f, 0f, _speed * Time.deltaTime);
		}
		
		// We apply rotation to movement to compensate for the rotated camera
		moveVector = Quaternion.AngleAxis(_adventureCamera.transform.localEulerAngles.y, Vector3.up) * moveVector;
		
		transform.position += moveVector;
		if (moveVector.magnitude > Mathf.Epsilon) {    // Where EPSILON is a very small number
			transform.rotation = Quaternion.LookRotation(moveVector);
		}

	}
}
