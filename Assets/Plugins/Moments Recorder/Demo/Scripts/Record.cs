﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Moments;

[RequireComponent(typeof(Recorder)), AddComponentMenu("")]
public class Record : MonoBehaviour
{
	Recorder m_Recorder;
	float m_Progress = 0f;
	string m_LastFile = "";
	bool m_IsSaving = false;
	public Font guiFont;
	public Text TopLine;
	public Text MidLine;	
	public Text BottomLine;	

	void Start()
	{
		// Get our Recorder instance (there can be only one per camera).
		m_Recorder = GetComponent<Recorder>();

		// If you want to change Recorder settings at runtime, use :
		//m_Recorder.Setup(autoAspect, width, height, fps, bufferSize, repeat, quality);

		// The Recorder starts paused for performance reasons, call Record() to start
		// saving frames to memory. You can pause it at any time by calling Pause().
		m_Recorder.Record();

		// Optional callbacks (see each function for more info).
		m_Recorder.OnPreProcessingDone = OnProcessingDone;
		m_Recorder.OnFileSaveProgress = OnFileSaveProgress;
		m_Recorder.OnFileSaved = OnFileSaved;
	}

	void OnProcessingDone()
	{
		// All frames have been extracted and sent to a worker thread for compression !
		// The Recorder is ready to record again, you can call Record() here if you don't
		// want to wait for the file to be compresse and saved.
		// Pre-processing is done in the main thread, but frame compression & file saving
		// has its own thread, so you can save multiple gif at once.

		m_IsSaving = true;
	}

	void OnFileSaveProgress(int id, float percent)
	{
		// This callback is probably not thread safe so use it at your own risks.
		// Percent is in range [0;1] (0 being 0%, 1 being 100%).
		m_Progress = percent * 100f;
	}

	void OnFileSaved(int id, string filepath)
	{
		// Our file has successfully been compressed & written to disk !
		m_LastFile = filepath;

		m_IsSaving = false;

		// Let's start recording again (note that we could do that as soon as pre-processing
		// is done and actually save multiple gifs at once, see OnProcessingDone().
		m_Recorder.Record();
	}

	void OnDestroy()
	{
		// Memory is automatically flushed when the Recorder is destroyed or (re)setup,
		// but if for some reason you want to do it manually, just call FlushMemory().
		//m_Recorder.FlushMemory();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.F2))
		{
			// Compress & save the buffered frames to a gif file. We should check the State
			// of the Recorder before saving, but for the sake of this example we won't, so
			// you'll see a warning in the console if you try saving while the Recorder is
			// processing another gif.
			m_Recorder.Save();
			m_Progress = 0f;
		}
		TopLine.text = ("Press [F2] to export the buffered frames to a gif file.").ToUpper();
		MidLine.text = ("Recorder State : " + m_Recorder.State.ToString()).ToUpper();
		if (m_IsSaving)
				{
					BottomLine.text = ("Progress Report : " + m_Progress.ToString("F2") + "%").ToUpper();
				}
		if (!string.IsNullOrEmpty(m_LastFile))
		{	
			BottomLine.text =("Last File Saved : " + m_LastFile).ToUpper();
		}
	}

/*	void OnGUI()
	{
		GUIStyle myToggle = new GUIStyle(GUI.skin.label);
		myToggle.normal.textColor = Color.green;
		myToggle.font = guiFont;
		myToggle.margin.top = 8;
		myToggle.fontSize = 24;
		GUILayout.BeginArea(AspectUtility.screenRect); 
		GUILayout.BeginHorizontal();
			GUILayout.Space(40f);
			GUILayout.BeginVertical();
				GUILayout.Space(40f);
				GUILayout.Label("Press [F8] to export the buffered frames to a gif file.",myToggle);
				GUILayout.Space(-30f);
				GUILayout.Label("Recorder State : " + m_Recorder.State.ToString(),myToggle);
				if (m_IsSaving)
				{	GUILayout.Space(-10f);
					GUILayout.Label("Progress Report : " + m_Progress.ToString("F2") + "%", myToggle);
				}
				if (!string.IsNullOrEmpty(m_LastFile))
				{	GUILayout.Space(-30f);
					GUILayout.Label("Last File Saved : " + m_LastFile, myToggle);
				}

			GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		GUILayout.EndArea();
	}*/

}
