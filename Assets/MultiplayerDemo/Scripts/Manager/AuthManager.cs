using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public class AuthManager : GenericSingletonClass<AuthManager>
{
    [Header("Firebase")]
    public FirebaseAuth auth;
    public FirebaseUser User;
    public Action<string> OnWarningUpdate;
    internal static string UserID
    {
        get => PlayerPrefs.GetString(nameof(UserID), string.Empty);
        private set
        {
            PlayerPrefs.SetString(nameof(UserID), value);
            PlayerPrefs.Save();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
        if (!string.IsNullOrWhiteSpace(UserID))
        {
            StartCoroutine(LoadNextScene());
        }
    }
    IEnumerator LoadNextScene()
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(1);
        while (!asyncOperation.isDone) yield return null;
    }
    internal void LogOut()
    {
        if (!string.IsNullOrWhiteSpace(UserID))
        {
            UserID = string.Empty;
        }
    }
    private void InitializeFirebase()
    {
        Debug.Log("Setting up Firebase Auth");
        auth = FirebaseAuth.DefaultInstance;
    }
    internal IEnumerator Login(string _email, string _password)
    {
        var LoginTask = auth.SignInWithEmailAndPasswordAsync(_email, _password);
        yield return new WaitUntil(predicate: () => LoginTask.IsCompleted);

        if (LoginTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {LoginTask.Exception}");
            FirebaseException firebaseEx = LoginTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Login Failed!";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.WrongPassword:
                    message = "Wrong Password";
                    break;
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
                case AuthError.UserNotFound:
                    message = "Account does not exist";
                    break;
            }
            OnWarningUpdate?.Invoke(message);
        }
        else
        {
            User = LoginTask.Result;
            UserID = User.UserId;
            Debug.LogFormat("User signed in successfully: {0} ({1})", User.DisplayName, User.Email);
            OnWarningUpdate?.Invoke("Logged In");
            StartCoroutine(LoadNextScene());
        }
    }

    internal IEnumerator Register(string _email, string _password, string _username, Action OnSuccess = null)
    {
        var RegisterTask = auth.CreateUserWithEmailAndPasswordAsync(_email, _password);
        yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);

        if (RegisterTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {RegisterTask.Exception}");
            FirebaseException firebaseEx = RegisterTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Register Failed!";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.WeakPassword:
                    message = "Weak Password";
                    break;
                case AuthError.EmailAlreadyInUse:
                    message = "Email Already In Use";
                    break;
            }
            OnWarningUpdate?.Invoke(message);
        }
        else
        {
            User = RegisterTask.Result;

            if (User != null)
            {
                UserProfile profile = new UserProfile { DisplayName = _username };

                var ProfileTask = User.UpdateUserProfileAsync(profile);
                yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

                if (ProfileTask.Exception != null)
                {
                    Debug.LogWarning(message: $"Failed to register task with {ProfileTask.Exception}");
                    FirebaseException firebaseEx = ProfileTask.Exception.GetBaseException() as FirebaseException;
                    AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                    OnWarningUpdate?.Invoke("Username Set Failed!");
                }
                else
                {
                    OnSuccess?.Invoke();
                    OnWarningUpdate?.Invoke(string.Empty);
                }
            }
        }
    }
}
