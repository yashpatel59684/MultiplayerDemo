using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AuthUIHandler : MonoBehaviour
{
    public GameObject loginUI;
    public GameObject registerUI;

    [Header("Login")]
    [SerializeField] TMP_InputField emailLoginField;
    [SerializeField] TMP_InputField passwordLoginField;
    [SerializeField] TMP_Text warningLoginText;

    [Header("Register")]
    [SerializeField] TMP_InputField usernameRegisterField;
    [SerializeField] TMP_InputField emailRegisterField;
    [SerializeField] TMP_InputField passwordRegisterField;
    [SerializeField] TMP_InputField passwordRegisterVerifyField;
    [SerializeField] TMP_Text warningRegisterText;

    private void Start()
    {
        AuthManager.Instance.OnWarningUpdate += OnWarningUpdate;
    }
    private void OnDestroy()
    {
        AuthManager.Instance.OnWarningUpdate += OnWarningUpdate;
    }

    private void OnWarningUpdate(string data)
    {
        warningLoginText.text = warningRegisterText.text = data;
    }

    public void LoginScreen()
    {
        loginUI.SetActive(true);
        registerUI.SetActive(false);
        warningLoginText.text = warningRegisterText.text = string.Empty;
    }
    public void RegisterScreen()
    {
        loginUI.SetActive(false);
        registerUI.SetActive(true);
        warningLoginText.text = warningRegisterText.text = string.Empty;
    }
    public void Login()
    {
        StartCoroutine(AuthManager.Instance.Login(emailLoginField.text, passwordLoginField.text));
    }
    public void Register()
    {
        if (usernameRegisterField.text == "")
        {
            warningRegisterText.text = "Missing Username";
        }
        else if (passwordRegisterField.text != passwordRegisterVerifyField.text)
        {
            warningRegisterText.text = "Password Does Not Match!";
        }
        else
        {
            StartCoroutine(AuthManager.Instance.Register(emailRegisterField.text, passwordRegisterField.text, usernameRegisterField.text, () => { LoginScreen(); }));
        }
    }
}