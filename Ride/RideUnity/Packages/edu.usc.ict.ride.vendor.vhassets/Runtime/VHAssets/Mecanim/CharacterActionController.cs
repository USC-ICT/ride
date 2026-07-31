using UnityEngine;
using UnityEngine.Networking;

namespace VHAssets
{
public class CharacterActionController : MonoBehaviour
{
    #region Variables
    [SerializeField] ICharacterController[] m_CharacterControllers;
    #endregion

    #region Functions

    #region Node
    public void Start()
    {
        m_CharacterControllers = VHUtils.FindObjectsByType<ICharacterController>();
    }

    public void NodHead(float speed, float amplitude, float numRepeats)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter[] controllerCharacters = controller.GetControlledCharacters();
            foreach (ICharacter character in controllerCharacters)
            {
                NodHead(controller, character, speed, amplitude, numRepeats);
            }
        }
    }

    public void NodHead(string charName, float speed, float amplitude, float numRepeats)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter character = controller.GetCharacter(charName);
            if (character != null)
            {
                NodHead(controller, character, amplitude, numRepeats, speed);
            }
        }
    }

    public void NodHead(ICharacterController controller, ICharacter character, float speed, float amplitude, float numRepeats)
    {
        controller.SBNod(character.CharacterName, amplitude, numRepeats, speed);
    }
    #endregion

    #region Shake
    public void ShakeHead(float speed, float amplitude, float numRepeats)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter[] controllerCharacters = controller.GetControlledCharacters();
            foreach (ICharacter character in controllerCharacters)
            {
                ShakeHead(controller, character, speed, amplitude, numRepeats);
            }
        }
    }

    public void ShakeHead(string charName, float speed, float amplitude, float numRepeats)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter character = controller.GetCharacter(charName);
            if (character != null)
            {
                ShakeHead(controller, character, amplitude, numRepeats, speed);
            }
        }
    }

    public void ShakeHead(ICharacterController controller, ICharacter character, float speed, float amplitude, float numRepeats)
    {
        controller.SBShake(character.CharacterName, amplitude, numRepeats, speed);
    }
    #endregion

    #region Gaze
    public void GazeAt(string gazeTargetName)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter[] controllerCharacters = controller.GetControlledCharacters();
            foreach (ICharacter character in controllerCharacters)
            {
                GazeAt(controller, character, gazeTargetName);
            }
        }
    }

    public void GazeAt(string charName, string gazeTargetName)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter character = controller.GetCharacter(charName);
            if (character != null)
            {
                GazeAt(controller, character, gazeTargetName);
            }
        }
    }

    public void GazeAt(ICharacterController controller, ICharacter character, string gazeTargetName)
    {
        controller.SBGaze(character.CharacterName, gazeTargetName);
    }

    public void StopGaze()
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter[] controllerCharacters = controller.GetControlledCharacters();
            foreach (ICharacter character in controllerCharacters)
            {
                StopGaze(controller, character);
            }
        }
    }

    public void StopGaze(string charName)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter character = controller.GetCharacter(charName);
            if (character != null)
            {
                StopGaze(controller, character);
            }
        }
    }

    public void StopGaze(ICharacterController controller, ICharacter character)
    {
        controller.SBStopGaze(character.CharacterName);
    }
    #endregion

    #region Animation
    public void PlayAnimation(string animName)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter[] controllerCharacters = controller.GetControlledCharacters();
            foreach (ICharacter character in controllerCharacters)
            {
                PlayAnimation(controller, character, animName);
            }
        }
    }

    public void PlayAnimation(string charName, string animName)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter character = controller.GetCharacter(charName);
            if (character != null)
            {
                PlayAnimation(controller, character, animName);
            }
        }
    }

    public void PlayAnimation(ICharacterController controller, ICharacter character, string animName)
    {
        controller.SBPlayAnim(character.CharacterName, animName);
    }
    #endregion

    #region Posture
    public void SetPosture(string animName)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter[] controllerCharacters = controller.GetControlledCharacters();
            foreach (ICharacter character in controllerCharacters)
            {
                SetPosture(controller, character, animName);
            }
        }
    }

    public void SetPosture(string charName, string animName)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter character = controller.GetCharacter(charName);
            if (character != null)
            {
                SetPosture(controller, character, animName);
            }
        }
    }

    public void SetPosture(ICharacterController controller, ICharacter character, string animName)
    {
        controller.SBPosture(character.CharacterName, animName, 0);
    }
    #endregion

    #region Face
    public void PlayViseme(string viseme, float weight)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter[] controllerCharacters = controller.GetControlledCharacters();
            foreach (ICharacter character in controllerCharacters)
            {
                PlayViseme(controller, character, viseme, weight);
            }
        }
    }

    public void PlayViseme(string charName, string viseme, float weight)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter character = controller.GetCharacter(charName);
            if (character != null)
            {
                PlayViseme(controller, character, viseme, weight);
            }
        }
    }

    public void PlayViseme(ICharacterController controller, ICharacter character, string viseme, float weight)
    {
        controller.SBPlayViseme(character.CharacterName, viseme, weight);
    }
    #endregion

    #region Audio
    public void PlayTTSSound(string charName, string audioPath)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter character = controller.GetCharacter(charName);
            if (character != null)
            {
                UnityWebRequest www = UnityWebRequest.Get(audioPath);
                if (character.Voice != null)
                {
                    VHUtils.PlayWWWSound(this, www, character.Voice, false);
                }
            }
        }
    }

    public void PlayPrerecordedSound(string audioId)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter[] controllerCharacters = controller.GetControlledCharacters();
            foreach (ICharacter character in controllerCharacters)
            {
                PlayPrerecordedSound(controller, character, audioId);
            }
        }
    }

    public void PlayPrerecordedSound(string charName, string audioId)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter character = controller.GetCharacter(charName);
            if (character != null)
            {
                PlayPrerecordedSound(controller, character, audioId);
            }
        }
    }

    public void PlayPrerecordedSound(ICharacterController controller, ICharacter character, string audioId)
    {
        controller.SBPlayAudio(character.CharacterName, audioId);
    }
    #endregion

    #region Saccades
    public void PlaySaccade(float direction, float magnitude, float duration)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter[] controllerCharacters = controller.GetControlledCharacters();
            foreach (ICharacter character in controllerCharacters)
            {
                PlaySaccade(controller, character, direction, magnitude, duration);
            }
        }
    }

    public void PlaySaccade(string charName, float direction, float magnitude, float duration)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter character = controller.GetCharacter(charName);
            if (character != null)
            {
                PlaySaccade(controller, character, direction, magnitude, duration);
            }
        }
    }

    public void PlaySaccade(ICharacterController controller, ICharacter character, float direction, float magnitude, float duration)
    {
        controller.SBSaccade(character.CharacterName, CharacterDefines.SaccadeType.Default, true, duration, magnitude, direction, magnitude);
    }

    public void SetSaccadeBehaviour(CharacterDefines.SaccadeType behaviour)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter[] controllerCharacters = controller.GetControlledCharacters();
            foreach (ICharacter character in controllerCharacters)
            {
                SetSaccadeBehaviour(controller, character, behaviour);
            }
        }
    }

    public void SetSaccadeBehaviour(string charName, CharacterDefines.SaccadeType behaviour)
    {
        foreach (ICharacterController controller in m_CharacterControllers)
        {
            ICharacter character = controller.GetCharacter(charName);
            if (character != null)
            {
                SetSaccadeBehaviour(controller, character, behaviour);
            }
        }
    }

    public void SetSaccadeBehaviour(ICharacterController controller, ICharacter character, CharacterDefines.SaccadeType behaviour)
    {
        controller.SBSaccade(character.CharacterName, behaviour, true, 1);

    }
    #endregion

    /*ICharacterController GetCharacter(string charName)
    {
        ICharacterController character = Array.Find<ICharacterController>(m_CharacterControllers, c => c.name == charName);
        if (character == null)
        {
            Debug.LogError("Failed to find character with name: " + charName);
        }
        return character;
    }*/
    #endregion
}
}
