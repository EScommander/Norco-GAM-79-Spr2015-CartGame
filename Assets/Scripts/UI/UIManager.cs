﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour 
{


	GameObject cart = null;
	int selectedIndex = 0;
	int prevIndex = -1;

	float offset = 0.374f;
	float spinSpeed = 30.0f;

	public RectTransform panel = null;
	public GameObject cartButton = null;

	public static UIManager _instance = null;

	public CameraMove sceneCamera = null;

	GameObject[] racers;
	private List<GameObject> characterButtons = new List<GameObject>();

	public List<GameObject> serverButtons = new List<GameObject>();
	public List<GameObject> trackButtons = new List<GameObject>();

	[HideInInspector]
	public int buttonSelected = -1;

	private bool zoomed = false;

	public GameObject tagObj = null;

	private int currentRow = 1;
	private int maxRow = 4;
	private int charsPerRow = 3;
	private bool dPadEnabled = true;

	EventSystem eventSystem;

	public GameObject serverUIPrefab = null;

	public enum e_UISubscreen {JOIN, CREATE};
	public e_UISubscreen current = e_UISubscreen.JOIN;

	public GameObject lobbyMenu = null;
	public GameObject joinMenu = null;
	public GameObject createMenu = null;
	public GameObject charSelectMenu = null;

	public GameObject serverList = null;
	public GameObject levelSelectList = null;
	public Text emptyServerList = null;

	private bool selectPlayerNum = false;
	private bool enableChangePlayerCount = true;
	private int minPlayers = 1;
	public Text minPlayersLabel = null;
	public GameObject minPlayersButton = null;

	public GameObject trackUIPrefab = null;

	public List<TrackData> tracks = new List<TrackData>();


	public static UIManager GetInstance()
	{
		//netManagerRef = NetworkManager.GetInstance ();

		if (_instance == null) 
		{
			GameObject go = new GameObject("_UIManager");
			_instance = (UIManager)go.AddComponent<UIManager>();
		}
	
		return _instance;
	}

	// Use this for initialization
	void Awake () 
	{
		eventSystem = EventSystem.current;
		_instance = this;

		Object[] objects = Resources.LoadAll("Carts");
		racers = new GameObject[objects.Length];
		for(int i = 0; i<objects.Length; i++)
		{
			//Cart GameObjects
			racers[i] = (GameObject)objects[i];

			//cart Buttons
			GameObject temp = Instantiate (cartButton);
			temp.transform.SetParent(panel.transform);
			temp.transform.name = racers [i].name;
			
			characterButtons.Add(temp);
			buttonSelected = 0;
			
			temp.GetComponent<RectTransform>().localScale = Vector3.one;
			
			PlayerData racerTempData = racers [i].GetComponent<PlayerData> ();					
			if (racerTempData != null)
				temp.transform.GetComponent<Image>().sprite = racers [i].GetComponent<PlayerData> ().characterSprite;
		}

		tagObj.transform.GetComponent<Tag>().charSprite.sprite = racers[0].GetComponent<PlayerData>().characterSprite;
		tagObj.transform.GetComponent<Tag>().characterName.text = racers[0].GetComponent<PlayerData>().name;
		tagObj.transform.GetComponent<Tag>().status.text = "Choosing...";

		//populate list of tracks
		foreach(TrackData track in tracks)
		{
			GameObject levelUIObject = (GameObject)Instantiate (trackUIPrefab, levelSelectList.transform.position, levelSelectList.transform.rotation);
			levelUIObject.transform.SetParent(levelSelectList.transform);
			levelUIObject.GetComponent<MapSelectUI>().SetUIData(track);
			levelUIObject.GetComponent<RectTransform>().localScale = Vector3.one;

			trackButtons.Add (levelUIObject);
			//serverButtons.Add (serverUIObject);	
		}


		//disables auto navigation on UI elements
		eventSystem.sendNavigationEvents = false;

		//always defaults to joining a server
		SwapUI("Join");
	}

	// Update is called once per frame
	void Update () 
	{
		switch(NetworkManager.GetInstance().current)
		{
		case NetworkManager.e_NetworkMode.SERVER_SELECT:
			if(current == e_UISubscreen.JOIN)
			{
				if(serverButtons.Count > 0)
				{ 
					emptyServerList.gameObject.SetActive(false);

					if(Input.GetAxis("D-PadUD") == 1 && dPadEnabled)         //move up
					{
						dPadEnabled = false;
						StartCoroutine(DPadInputServer(1));
						
					}
					else if(Input.GetAxis("D-PadUD") == -1 && dPadEnabled)   //move down
					{
						dPadEnabled = false;
						StartCoroutine(DPadInputServer(-1));
					}

					eventSystem.SetSelectedGameObject(serverButtons[buttonSelected]);
				}
				else emptyServerList.gameObject.SetActive(true);

				if(Input.GetButtonDown("Fire1"))
				{
					SwapUI ("Create");
					selectPlayerNum = false;
					buttonSelected = 0;
					current = e_UISubscreen.CREATE;
				}

				if(Input.GetButtonDown("Submit"))
				{
					NetworkManager.GetInstance().JoinServer(buttonSelected, serverButtons[buttonSelected].GetComponent<GameServer>().mapName.text);
					buttonSelected = 0;
					SwapUI("Select");
				}
			}
			else
			{
				if(selectPlayerNum)
				{
					if(Input.GetAxis("D-PadUD") == 1 && dPadEnabled)         //move up
					{
						//dPadEnabled = false;
						//StartCoroutine(DPadInputServer(1));
						StartCoroutine(ChangePlayerReq(1));
						dPadEnabled = false;

						
					}
					else if(Input.GetAxis("D-PadUD") == -1 && dPadEnabled)   //move down
					{
						//dPadEnabled = false;
						//StartCoroutine(DPadInputServer(-1));
						StartCoroutine(ChangePlayerReq(-1));
						dPadEnabled = false;
					}

					minPlayersLabel.text = minPlayers.ToString();
					//eventSystem.SetSelectedGameObject(serverButtons[buttonSelected]);
				}
				else if(trackButtons.Count > 0 )
				{
					Debug.Log("selecting Maps");
					if(Input.GetAxis("D-PadUD") == 1 && dPadEnabled)         //move up
					{
						dPadEnabled = false;
						StartCoroutine(DPadInputMaps(1));
						
					}
					else if(Input.GetAxis("D-PadUD") == -1 && dPadEnabled)   //move down
					{
						dPadEnabled = false;
						StartCoroutine(DPadInputMaps(-1));
					}
					
					eventSystem.SetSelectedGameObject(trackButtons[buttonSelected]);
				}

				if(Input.GetButtonDown("Fire1"))
				{
					SwapUI ("Join");
					buttonSelected = 0;
					current = e_UISubscreen.JOIN;
				}

				if(Input.GetButtonDown("Submit") && !selectPlayerNum)
				{
					selectPlayerNum = true;
					eventSystem.SetSelectedGameObject(minPlayersButton);
				}
				else if(Input.GetButtonDown("Submit"))
				{					
					NetworkManager.GetInstance().CreateServer(trackButtons[buttonSelected].GetComponent<MapSelectUI>().actualSceneName, minPlayers);
					buttonSelected = 0;
					SwapUI("Select");
				}
			}
			break;
		case NetworkManager.e_NetworkMode.CHARACTER_SELECT:
			// keyboard and joystick inputs
			if(Input.GetAxis("D-PadLR") == 1 && dPadEnabled)         //move right
			{
				dPadEnabled = false;
				StartCoroutine(DPadInputCharacter(1, false));

			}
			else if(Input.GetAxis("D-PadLR") == -1 && dPadEnabled)   //move left
			{
				dPadEnabled = false;
				StartCoroutine(DPadInputCharacter(-1, false));
			}

			if(Input.GetAxis("D-PadUD") == 1 && dPadEnabled)         //move up
			{
				dPadEnabled = false;
				StartCoroutine(DPadInputCharacter(1, true));

			}
			else if(Input.GetAxis("D-PadUD") == -1 && dPadEnabled)   //move down
			{
				dPadEnabled = false;
				StartCoroutine(DPadInputCharacter(-1, true));

			}

			//zoom
			if(Input.GetButtonDown ("Zoom"))
			{
				zoomed = !zoomed;
			}
			
			if(Input.GetButtonDown("Submit"))
			{
				charSelectMenu.SetActive(false);
				StartRace();
			}

			eventSystem.SetSelectedGameObject(characterButtons[buttonSelected]);

			if(cart != null)
				cart.transform.Rotate (Vector3.up * spinSpeed * Time.deltaTime);
			else 
			{
				cart = (GameObject)Instantiate(racers[0], Vector3.zero + Vector3.up * offset, racers[0].transform.rotation);
				cart.name = racers[0].name;
				prevIndex = 0;
			}

			if(zoomed)
				sceneCamera.SetPosition(cart.GetComponent<PlayerData>().zoomedView);
			else sceneCamera.SetPosition(cart.GetComponent<PlayerData>().regularView);

			break;

			case NetworkManager.e_NetworkMode.MAP_SELECT:
				break;

			case NetworkManager.e_NetworkMode.RACE:
				break;
		}
	

	}

	public void CreateServerButton(string serverName, string mapName, int currPlayers, int maxPlayers)
	{
		GameObject serverUIObject = (GameObject)Instantiate (serverUIPrefab, serverList.transform.position, serverList.transform.rotation);
		serverUIObject.transform.SetParent(serverList.transform);
		serverUIObject.GetComponent<GameServer>().SetUIData(serverName, mapName, currPlayers, maxPlayers);
		serverUIObject.GetComponent<RectTransform>().localScale = Vector3.one;

		serverButtons.Add (serverUIObject);

	}

	public void SwitchKart(string name)
	{
		for(int i = 0; i < racers.Length; i++)
		{
			if(racers[i].name == name)
			{
				GameObject prevCart = cart;
				cart = (GameObject)Instantiate(racers[i], Vector3.zero + Vector3.up * offset, prevCart.transform.rotation);
				cart.name = racers[i].name;
				Destroy (prevCart);
				buttonSelected = i;
				tagObj.transform.GetComponent<Tag>().charSprite.sprite = racers[i].GetComponent<PlayerData>().characterSprite;
				tagObj.transform.GetComponent<Tag>().characterName.text = racers[i].GetComponent<PlayerData>().name;
				tagObj.transform.GetComponent<Tag>().status.text = "Choosing...";
			}
		}
	}
	
	public void SwapUI(string name)
	{
		switch(name)
		{
		case "Join":
			lobbyMenu.SetActive(true);
			joinMenu.SetActive(true);
			createMenu.SetActive(false);
			charSelectMenu.SetActive(false);
			break;
		case "Create":
			lobbyMenu.SetActive(true);
			joinMenu.SetActive(false);
			createMenu.SetActive(true);
			charSelectMenu.SetActive(false);
			break;
		case "Select":
			lobbyMenu.SetActive(false);
			joinMenu.SetActive(false);
			createMenu.SetActive(false);
			charSelectMenu.SetActive(true);
			break;
		}
	}

	public IEnumerator ChangePlayerReq(int i)
	{
		yield return new WaitForSeconds(0.4f);

		minPlayers += i;

		if(minPlayers < 1)
			minPlayers = 1;
		dPadEnabled = true;
	}

	public IEnumerator DPadInputMaps(int value)
	{
		Debug.Log ("changng buttons" + value);
		if(value == 1)  // go up
		{
			if(buttonSelected-1 < 0)
				buttonSelected = trackButtons.Count-1;
			else buttonSelected -= 1;
			
		}
		else // go down
		{
			if(buttonSelected+1 >= trackButtons.Count)
				buttonSelected = 0;
			else buttonSelected += 1;
		}

		Debug.Log (trackButtons [buttonSelected].GetComponentInChildren<Text> ().text);

		eventSystem.SetSelectedGameObject (trackButtons [buttonSelected]);		
		yield return new WaitForSeconds(0.3f);
		dPadEnabled = true;
	}

	public IEnumerator DPadInputServer(int value)
	{

		if(value == 1)  // go up
		{
			if(buttonSelected-1 < 0)
				buttonSelected = serverButtons.Count-1;
			else buttonSelected -= 1;

		}
		else // go down
		{
			if(buttonSelected+1 >= serverButtons.Count)
				buttonSelected = 0;
			else buttonSelected += 1;
		}

		
		yield return new WaitForSeconds(0.3f);
		dPadEnabled = true;
	}
		
	public IEnumerator DPadInputCharacter(int value, bool isVertical)
	{
		if(!isVertical)
		{
			if(value == 1)  // go right in button list
			{
				if(buttonSelected + 1 < racers.Length-1)
				{
					buttonSelected = (buttonSelected + 1) % 3 + charsPerRow*(currentRow-1);//(buttonSelected + (charsPerRow * currentRow) + 1) % (charsPerRow * currentRow);
					SwitchKart(characterButtons[buttonSelected].name);
				}
				else
				{
					buttonSelected = charsPerRow * (currentRow-1);
					SwitchKart(characterButtons[buttonSelected].name);
				}
			}
			else // go left in button list
			{
				if((buttonSelected + 2) % 3 + charsPerRow*(currentRow-1) < racers.Length-1)
				{
					buttonSelected = (buttonSelected + 2) % 3 + charsPerRow*(currentRow-1); //(buttonSelected + (charsPerRow * currentRow) -1) % (charsPerRow * currentRow);
					SwitchKart(characterButtons[buttonSelected].name);
				}
				else
				{
					buttonSelected = racers.Length -1;
					SwitchKart(characterButtons[buttonSelected].name);
				}
			}
		}
		else
		{
			if(value == -1)
			{
				if(buttonSelected + 3 > racers.Length-1)
				{
					buttonSelected = (buttonSelected + 3) % 3;
					SwitchKart(characterButtons[buttonSelected].name);
					currentRow = 1;
				}
				else
				{
					buttonSelected = (buttonSelected + 3) % (charsPerRow * maxRow);//(buttonSelected + characterButtons.Count + 3) % (characterButtons.Count);
					SwitchKart(characterButtons[buttonSelected].name);
					currentRow++;
				}
			}
			else
			{
				if((buttonSelected + (charsPerRow * maxRow) - 3) % (charsPerRow * maxRow)  > racers.Length -1)
				{
					buttonSelected = (buttonSelected + (charsPerRow * maxRow) - 6) % (charsPerRow * maxRow);
					SwitchKart(characterButtons[buttonSelected].name);
					currentRow = maxRow -1;
				}
				else
				{
					buttonSelected = (buttonSelected + (charsPerRow * maxRow) - 3) % (charsPerRow * maxRow);
					SwitchKart(characterButtons[buttonSelected].name);
					if(currentRow - 1 < 1)
						currentRow = maxRow;
					else currentRow--;
				}
			}
		}

		yield return new WaitForSeconds(0.3f);
		dPadEnabled = true;
	}

	public void SelectCharacter()
	{
		NetworkManager.GetInstance().setRacer(cart.GetComponent<PlayerData>().cartPrefab.gameObject);
	}

	public void StartRace()
	{
		SelectCharacter ();
	}
}
