using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class devilishEggs : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable[] cookingButtons;
    public KMSelectable[] numberButtons;
    public Color[] colors;
    public Color cold;
    public Color hot;
    public Color ledGreen;
    public Color ledRed;
    public Color ledOff;
    public Renderer thermometerLED;
    public Renderer[] stage1LEDs;
    public Renderer[] stage2LEDs;
    public Transform[] topPivots;
    public Transform[] bottomPivots;
    public Transform mercury;
    public Transform prism;
    public Renderer mercuryRender;
    public TextMesh[] thermometerTexts;
    public Renderer[] topDots;
    public Renderer[] bottomDots;
    public Renderer[] cookingButtonRenders;
    public Renderer[] cookingButtonIcons;
    public Renderer[] numberButtonsRenders;
    public TextMesh[] numberButtonsTexts;
    public TextMesh[] prismTexts;
    public Texture[] cookingTextures;

    private DERotation[] topRotations = new DERotation[6];
    private DERotation[] bottomRotations = new DERotation[6];
    private DEColor topColor;
    private DEColor bottomColor;
    private DEColor[] cookingButtonColors = new DEColor[4];
    private DEColor[] numberButtonColors = new DEColor[4];
    private DECooking[] cookingButtonLabels = new DECooking[4];
    private int[] numberButtonLabels = new int[4];
    private int[] temperatures = new int[8];
    private int[] temperatureLabels = new int[5];

    private static readonly string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly string vowels = "AEIOUY";
    private static readonly string consonants = "BCDFGHJKLMNPQRSTVWXZ";
    private static readonly string[] associations = new string[4] { "OQRFJC", "TZDVUNS", "EGPIWY", "MXAKBHL" };
    private static readonly string[] cookingNames = new string[4] { "Sunny Side Up", "Fried", "Scrambled", "Boiled" };
    private static readonly string[] ordinals = new string[8] { "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth" };

    private int[] prismDigits = new int[8];
    private char[] prismLetters = new char[8];
    private int[] stage1Solution = new int[8];
    private DECooking[] stage2Solution = new DECooking[4];
    private DEInstruction[] instructions = new DEInstruction[4];

    private bool prismIsCcw;
    private bool stage2;
    private bool thermometerAnimating;
    private int enteringStage;
    private float prismSpeed = 20f;
    private Coroutine topEggRotating;
    private Coroutine bottomEggRotating;
    private Coroutine prismSpinning;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in numberButtons)
            button.OnInteract += delegate () { PressNumberButton(button); return false; };
        foreach (KMSelectable button in cookingButtons)
            button.OnInteract += delegate () { PressCookingButton(button); return false; };
    }

    void Start()
    {
        prismIsCcw = rnd.Range(0, 2) == 0;
        for (int i = 0; i < 6; i++)
        {
            topRotations[i] = (DERotation) rnd.Range(0, 16);
            bottomRotations[i] = (DERotation) rnd.Range(0, 16);
        }
        topColor = (DEColor) rnd.Range(0, 4);
        bottomColor = (DEColor) rnd.Range(0, 4);
        while (bottomColor == topColor)
            bottomColor = (DEColor) rnd.Range(0, 4);
        cookingButtonColors = Enumerable.Range(0, 4).ToList().Shuffle().Select(x => (DEColor) x).ToArray();
        numberButtonColors = Enumerable.Range(0, 4).ToList().Shuffle().Select(x => (DEColor) x).ToArray();
        cookingButtonLabels = Enumerable.Range(0, 4).ToList().Shuffle().Select(x => (DECooking) x).ToArray();
        numberButtonLabels = Enumerable.Range(1, 4).ToList().Shuffle().ToArray();
        temperatures = new int[8].Select(x => x = rnd.Range(0, 5)).ToArray();
        while (temperatures[0] == 4)
            temperatures[0] = rnd.Range(0, 4);
        for (int i = 1; i < 8; i++)
            while (temperatures[i] == temperatures[i - 1])
                temperatures[i] = rnd.Range(0, 5);
        temperatureLabels = Enumerable.Range(0, 5).ToList().Shuffle().ToArray();
        for (int i = 0; i < 4; i++)
        {
            topDots[i].material.color = colors[(int) topColor];
            bottomDots[i].material.color = colors[(int) bottomColor];
            numberButtonsRenders[i].material.color = colors[(int) numberButtonColors[i]];
            numberButtonsTexts[i].text = numberButtonLabels[i].ToString();
        }
        Debug.LogFormat("[Devilish Eggs #{0}] The top egg's rotations are {1}.", moduleId, topRotations.Join(", "));
        Debug.LogFormat("[Devilish Eggs #{0}] The bottom egg's rotations are {1}.", moduleId, bottomRotations.Join(", "));
        Debug.LogFormat("[Devilish Eggs #{0}] The dots on the top egg are {1}, and the dots on the bottom egg are {2}.", moduleId, topColor, bottomColor);
        Debug.LogFormat("[Devilish Eggs #{0}] The number buttons have the labels {1} and the colors {2}.", moduleId, numberButtonLabels.Join(", "), numberButtonColors.Join(", "));
        Debug.LogFormat("[Devilish Eggs #{0}] The thermometer labels from bottom to top are {1}.", moduleId, temperatureLabels.Select(x => 5 * x + 5).Join(", "));

        for (int i = 0; i < 8; i++)
        {
            prismDigits[i] = rnd.Range(0, 10);
            prismLetters[i] = alphabet.PickRandom();
        }
        prismTexts[0].text = prismDigits.Join(" ");
        prismTexts[1].text = prismLetters.Join(" ");
        Debug.LogFormat("[Devilish Eggs #{0}] The string of numbers on the module is {1}.", moduleId, prismDigits.Join(""));
        Debug.LogFormat("[Devilish Eggs #{0}] The string of letters without angle brackets on the module is {1}.", moduleId, prismLetters.Join(""));

        for (int i = 0; i < 6; i++)
        {
            switch (topRotations[i])
            {
                case DERotation.T270CCW:
                case DERotation.W180CW:
                    prismDigits = Swap(prismDigits, 1, 3);
                    break;
                case DERotation.T90CW:
                case DERotation.W90CCW:
                    prismDigits = Shift(prismDigits, 4);
                    break;
                case DERotation.W360CW:
                case DERotation.T90CCW:
                    prismDigits = Swap(prismDigits, 2, 6);
                    break;
                case DERotation.T180CCW:
                case DERotation.W270CCW:
                    Array.Reverse(prismDigits);
                    break;
                case DERotation.T180CW:
                case DERotation.W90CW:
                    prismDigits = Shift(prismDigits, 5);
                    break;
                case DERotation.T360CCW:
                case DERotation.W360CCW:
                    prismDigits = Swap(prismDigits, 7, 8);
                    break;
                case DERotation.W270CW:
                case DERotation.T270CW:
                    prismDigits = Swap(prismDigits, 1, 4);
                    prismDigits = Swap(prismDigits, 2, 3);
                    break;
                case DERotation.W180CCW:
                case DERotation.T360CW:
                    prismDigits = Swap(prismDigits, 5, 8);
                    prismDigits = Swap(prismDigits, 6, 7);
                    break;
            }

            switch (bottomRotations[i])
            {
                case DERotation.T270CCW:
                case DERotation.W180CW:
                    prismLetters = Swap(prismLetters, 1, 3);
                    break;
                case DERotation.T90CW:
                case DERotation.W90CCW:
                    prismLetters = Shift(prismLetters, 4);
                    break;
                case DERotation.W360CW:
                case DERotation.T90CCW:
                    prismLetters = Swap(prismLetters, 2, 6);
                    break;
                case DERotation.T180CCW:
                case DERotation.W270CCW:
                    Array.Reverse(prismLetters);
                    break;
                case DERotation.T180CW:
                case DERotation.W90CW:
                    prismLetters = Shift(prismLetters, 5);
                    break;
                case DERotation.T360CCW:
                case DERotation.W360CCW:
                    prismLetters = Swap(prismLetters, 7, 8);
                    break;
                case DERotation.W270CW:
                case DERotation.T270CW:
                    prismLetters = Swap(prismLetters, 1, 4);
                    prismLetters = Swap(prismLetters, 2, 3);
                    break;
                case DERotation.W180CCW:
                case DERotation.T360CW:
                    prismLetters = Swap(prismLetters, 5, 8);
                    prismLetters = Swap(prismLetters, 6, 7);
                    break;
            }
        }

        Debug.LogFormat("[Devilish Eggs #{0}] After modifications using the rotations of the top egg, the new string of digits is {1}.", moduleId, prismDigits.Join(""));
        Debug.LogFormat("[Devilish Eggs #{0}] After modifications using the rotations of the bottom egg, the new string of letters without angle brackets is {1}.", moduleId, prismLetters.Join(""));
        for (int i = 0; i < 8; i++)
        {
            var letters = vowels.Contains(prismLetters[i]) ? vowels : consonants;
            var oldLetter = prismLetters[i];
            prismLetters[i] = letters[(letters.IndexOf(prismLetters[i]) + prismDigits[i]) % letters.Length];
            Debug.LogFormat("[Devilish Eggs #{0}] {1} is a {2}. Shifted by {3}, it becomes {4}.", moduleId, oldLetter, vowels.Contains(oldLetter) ? "vowel" : "consonant", prismDigits[i], prismLetters[i]);
            stage1Solution[i] = Array.IndexOf(associations, associations.First(x => x.Contains(prismLetters[i]))) + 1;
            Debug.LogFormat("[Devilish Eggs #{0}] This letter corresponds to {1}.", moduleId, stage1Solution[i]);
        }
        Debug.LogFormat("[Devilish Eggs #{0}] The final sequence of numbers to input is {1}.", moduleId, stage1Solution.Join(", "));

        var str = "";
        for (int i = 0; i < 8; i++)
            str += "?|[]*&%^#@!(){}".PickRandom();
        prismTexts[2].text = "<" + str + ">";
        foreach (Renderer icon in cookingButtonIcons)
            icon.gameObject.SetActive(false);
        for (int i = 0; i < 5; i++)
            thermometerTexts[i].text = ((temperatureLabels[i] + 1) * 5).ToString();

        StartCoroutine(StartRotations());
        prismSpinning = StartCoroutine(PrismCycle());
    }

    void PressNumberButton(KMSelectable button)
    {
        button.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        if (moduleSolved || thermometerAnimating || stage2)
            return;
        var ix = Array.IndexOf(numberButtons, button);
        Debug.LogFormat("[Devilish Eggs #{0}] You pressed the button with the label {1}.", moduleId, numberButtonLabels[ix]);
        if (numberButtonLabels[ix] != stage1Solution[enteringStage])
        {
            Debug.LogFormat("[Devilish Eggs #{0}] That was incorrect. Strike!", moduleId);
            module.HandleStrike();
        }
        else
        {
            StartCoroutine(TemperatureChange(temperatures[enteringStage]));
            Debug.LogFormat("[Devilish Eggs #{0}] That was correct. The temperature went to the position of {1}, which has the label {2}.", moduleId, (temperatures[enteringStage] + 1) * 5, (temperatureLabels[temperatures[enteringStage]] + 1) * 5);
            stage1LEDs[enteringStage].material.color = ledGreen;
            enteringStage++;
        }
        if (enteringStage == 8)
            StageTwo();
    }

    void PressCookingButton(KMSelectable button)
    {
        button.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        if (moduleSolved || !stage2)
            return;
        var ix = Array.IndexOf(cookingButtons, button);
        Debug.LogFormat("[Devilish Eggs #{0}] You pressed the {1} button.", moduleId, cookingNames[(int) cookingButtonLabels[ix]]);
        if (cookingButtonLabels[ix] != stage2Solution[enteringStage])
        {
            Debug.LogFormat("[Devilish Eggs #{0}] That was incorrect. Strike!", moduleId);
            module.HandleStrike();
        }
        else
        {
            Debug.LogFormat("[Devilish Eggs #{0}] That was correct.", moduleId);
            stage2LEDs[enteringStage].material.color = ledGreen;
            enteringStage++;
        }
        if (enteringStage == 4)
        {
            module.HandlePass();
            moduleSolved = true;
            Debug.LogFormat("[Devilish Eggs #{0}] Four cooking buttons were correctly pressed. Module solved!", moduleId);
            Debug.LogFormat("[Devilish Eggs #{0}] Eggstravagent work.", moduleId);
            thermometerLED.material.color = ledGreen;
            foreach (TextMesh prismText in prismTexts)
                prismText.text = "EGG EGG EGG";
            StartCoroutine(SlowDownPrism());
            for (int i = 0; i < 4; i++)
            {
                StartCoroutine(HideDot(topDots[i].transform));
                StartCoroutine(HideDot(bottomDots[i].transform));
            }
            StopCoroutine(topEggRotating);
            StopCoroutine(bottomEggRotating);
            StartCoroutine(ResetEggs());
        }
    }

    void StageTwo()
    {
        stage2 = true;
        enteringStage = 0;
        StartCoroutine(ResetTemperature());

        for (int i = 0; i < 4; i++)
        {
            cookingButtonIcons[i].gameObject.SetActive(true);
            cookingButtonIcons[i].material.mainTexture = cookingTextures[(int) cookingButtonLabels[i]];
            cookingButtonRenders[i].material.color = colors[(int) cookingButtonColors[i]];
        }
        Debug.LogFormat("[Devilish Eggs #{0}] The cooking buttons have the labels {1} and the colors {2}.", moduleId, cookingButtonLabels.Select(x => cookingNames[(int) x]).Join(", "), cookingButtonColors.Join(", "));

        for (int i = 0; i < 4; i++)
        {
            instructions[i] = (DEInstruction) rnd.Range(0, 17);
            switch (instructions[i])
            {
                case DEInstruction.SS:
                    stage2Solution[i] = DECooking.sunnySideUp;
                    break;
                case DEInstruction.FR:
                    stage2Solution[i] = DECooking.fried;
                    break;
                case DEInstruction.SC:
                    stage2Solution[i] = DECooking.scrambled;
                    break;
                case DEInstruction.BL:
                    stage2Solution[i] = DECooking.boiled;
                    break;
                case DEInstruction.CM:
                    stage2Solution[i] = cookingButtonLabels[Array.IndexOf(cookingButtonColors, DEColor.magenta)];
                    break;
                case DEInstruction.CO:
                    stage2Solution[i] = cookingButtonLabels[Array.IndexOf(cookingButtonColors, DEColor.orange)];
                    break;
                case DEInstruction.CG:
                    stage2Solution[i] = cookingButtonLabels[Array.IndexOf(cookingButtonColors, DEColor.green)];
                    break;
                case DEInstruction.CC:
                    stage2Solution[i] = cookingButtonLabels[Array.IndexOf(cookingButtonColors, DEColor.cyan)];
                    break;
                case DEInstruction.CT:
                    stage2Solution[i] = cookingButtonLabels[Array.IndexOf(cookingButtonColors, topColor)];
                    break;
                case DEInstruction.CB:
                    stage2Solution[i] = cookingButtonLabels[Array.IndexOf(cookingButtonColors, bottomColor)];
                    break;
                case DEInstruction.MA:
                    stage2Solution[i] = cookingButtonLabels[Array.IndexOf(cookingButtonColors, numberButtonColors[Array.IndexOf(numberButtonLabels, 1)])];
                    break;
                case DEInstruction.MB:
                    stage2Solution[i] = cookingButtonLabels[Array.IndexOf(cookingButtonColors, numberButtonColors[Array.IndexOf(numberButtonLabels, 2)])];
                    break;
                case DEInstruction.MC:
                    stage2Solution[i] = cookingButtonLabels[Array.IndexOf(cookingButtonColors, numberButtonColors[Array.IndexOf(numberButtonLabels, 3)])];
                    break;
                case DEInstruction.MD:
                    stage2Solution[i] = cookingButtonLabels[Array.IndexOf(cookingButtonColors, numberButtonColors[Array.IndexOf(numberButtonLabels, 4)])];
                    break;
                case DEInstruction.RE:
                    stage2Solution[i] = i == 0 ? cookingButtonLabels[Array.IndexOf(cookingButtonColors, DEColor.orange)] : stage2Solution[i - 1];
                    break;
                case DEInstruction.IS:
                    stage2Solution[i] = bomb.GetSolvableModuleNames().Contains("egg") ? DECooking.sunnySideUp : DECooking.scrambled;
                    break;
                case DEInstruction.IB:
                    stage2Solution[i] = bomb.GetSolvableModuleNames().Contains("The Cube") || bomb.GetSolvableModuleNames().Contains("Unfair Cipher") ? DECooking.fried : DECooking.boiled;
                    break;
            }
        }

        var grid = new char[25];
        var gridList = alphabet.ToList();
        gridList.Remove('X');
        for (int i = 0; i < 8; i++)
        {
            var row = temperatures[i];
            var column = temperatureLabels[temperatures[i]];
            var letter = gridList[row * 5 + column];
            gridList.Remove(letter);
            gridList.Add(letter);
            Debug.LogFormat("[Devilish Eggs #{0}] The {1} temperature reading was at {2} with the label {3}. Moving {4}.", moduleId, ordinals[i], (temperatures[i] + 1) * 5, (temperatureLabels[temperatures[i]] + 1) * 5, letter);
        }

        switch (((int) bottomRotations[5]) / 4)
        {
            case 0:
                grid = gridList.Select((_, i) => i % 5 == 0 ? gridList[i + 4] : gridList[i - 1]).ToArray();
                break;
            case 1:
                grid = gridList.Select((_, i) => i % 5 == 4 ? gridList[i - 4] : gridList[i + 1]).ToArray();
                break;
            case 2:
                grid = gridList.Select((_, i) => i / 5 == 0 ? gridList[i + 20] : gridList[i - 5]).ToArray();
                break;
            case 3:
                grid = gridList.Select((_, i) => i / 5 == 4 ? gridList[i - 20] : gridList[i + 5]).ToArray();
                break;
        }

        var str = "";
        var goal = instructions.Join("");
        var boxes = new DERotation[8][]
        {
            new DERotation[2] { DERotation.T90CW, DERotation.W270CW },
            new DERotation[2] { DERotation.T90CCW, DERotation.W270CCW },
            new DERotation[2] { DERotation.T180CW, DERotation.W360CW },
            new DERotation[2] { DERotation.T180CCW, DERotation.W360CCW },
            new DERotation[2] { DERotation.T270CW, DERotation.W90CW },
            new DERotation[2] { DERotation.T270CCW, DERotation.W90CCW },
            new DERotation[2] { DERotation.T360CW, DERotation.W180CW },
            new DERotation[2] { DERotation.T360CCW, DERotation.W180CCW }
        };
        for (int i = 0; i < 8; i++)
        {
            var movement = (DEMovement) ((Array.IndexOf(boxes, boxes.First(x => x.Contains((i < 4 ? topRotations : bottomRotations)[i % 4]))) + prismDigits[i]) % 8);
            Debug.LogFormat("[Devilish Eggs #{0}] Using the rotation {1}, moving forward by {2}. Using transformation: {3}.", moduleId, (i < 4 ? topRotations : bottomRotations)[i % 4], prismDigits[i], movement);
            switch (movement)
            {
                case DEMovement.stayInPlace:
                    str += goal[i];
                    break;
                case DEMovement.rotateCW:
                    str += grid[Process(Array.IndexOf(grid, goal[i]), DEMovement.rotateCCW)];
                    break;
                case DEMovement.rotateCCW:
                    str += grid[Process(Array.IndexOf(grid, goal[i]), DEMovement.rotateCW)];
                    break;
                default:
                    str += grid[Process(Array.IndexOf(grid, goal[i]), movement)];
                    break;
            }
        }
        prismTexts[2].text = "<" + str.ToCharArray().Join(" ") + ">";

        for (int i = 0; i < 4; i++)
            Debug.LogFormat("[Devilish Eggs #{0}] The {1} instruction is {2}. Press the {3} button.", moduleId, ordinals[i], instructions[i], cookingNames[(int) stage2Solution[i]]);
    }

    IEnumerator StartRotations()
    {
        var topFirst = rnd.Range(0, 2) == 0;
        yield return new WaitForSeconds(rnd.Range(.5f, 2f));
        if (topFirst)
            topEggRotating = StartCoroutine(RotationCycle(topPivots, topRotations));
        else
            bottomEggRotating = StartCoroutine(RotationCycle(bottomPivots, bottomRotations));
        yield return new WaitForSeconds(rnd.Range(.5f, 2f));
        if (topFirst)
            bottomEggRotating = StartCoroutine(RotationCycle(bottomPivots, bottomRotations));
        else
            topEggRotating = StartCoroutine(RotationCycle(topPivots, topRotations));
    }

    IEnumerator RotationCycle(Transform[] pivots, DERotation[] rotations)
    {
        while (true)
        {
            for (int i = 0; i < 6; i++)
            {
                var id = (int) rotations[i];
                var elapsed = 0f;
                var duration = 1f * (id % 4 + 1);
                var isTurn = id < 8;
                var egg = isTurn ? pivots[0] : pivots[1];
                var startRotation = egg.localEulerAngles;
                var angle = 90f * (id % 4 + 1);
                var isCw = id / 4 == 0 || id / 4 == 2;
                if (isCw)
                    angle *= -1f;
                var start = isTurn ? startRotation.z : startRotation.y;
                var end = start + angle;
                while (elapsed < duration)
                {
                    var cord =  Mathf.Lerp(start, end, elapsed / duration);
                    egg.localEulerAngles = isTurn ? new Vector3(startRotation.x, startRotation.y, cord) : new Vector3(startRotation.x, cord, startRotation.z);
                    yield return null;
                    elapsed += Time.deltaTime;
                }
                egg.localEulerAngles = isTurn ? new Vector3(startRotation.x, startRotation.y, end) : new Vector3(startRotation.x, end, startRotation.z);
                yield return new WaitForSeconds(.5f);
            }
            yield return new WaitForSeconds(2f);
        }
    }

    IEnumerator TemperatureChange(int targetTemp)
    {
        var decimalThings = new float[] { .125f, .25f, .5f, .75f, 1f };
        var scales = new float[] { .1208972f, .2417942f, .4835888f, .7253833f, .9671777f };
        var positions = new float[] { -.875f, -.75f, -.5f, -.25f, 0f };
        var colors = new Color[5];
        for (int i = 0; i < 5; i++)
        {
            colors[i] = new Color(
                Mathf.Lerp(cold.r, hot.r, decimalThings[i]),
                Mathf.Lerp(cold.g, hot.g, decimalThings[i]),
                Mathf.Lerp(cold.b, hot.b, decimalThings[i])
            );
        }
        var startScale = mercury.localScale.y;
        var endScale = scales[targetTemp];
        var startPos = mercury.localPosition.y;
        var endPos = positions[targetTemp];
        var startColor = mercuryRender.material.color;
        var endColor = colors[targetTemp];
        var elapsed = 0f;
        var duration = 2f;
        thermometerAnimating = true;
        if (!moduleSolved)
            thermometerLED.material.color = ledRed;
        while (elapsed < duration)
        {
            mercury.localPosition = new Vector3(0f, Mathf.Lerp(startPos, endPos, elapsed / duration), .062f);
            mercury.localScale = new Vector3(.9202852f, Mathf.Lerp(startScale, endScale, elapsed / duration), 1f);
            mercuryRender.material.color = new Color(
                Mathf.Lerp(startColor.r, endColor.r, elapsed / duration),
                Mathf.Lerp(startColor.g, endColor.g, elapsed / duration),
                Mathf.Lerp(startColor.b, endColor.b, elapsed / duration)
            );
            yield return null;
            elapsed += Time.deltaTime;
        }
        mercury.localPosition = new Vector3(0f, endPos, .062f);
        mercury.localScale = new Vector3(.9202852f, endScale, 1f);
        mercuryRender.material.color = endColor;
        if (!moduleSolved)
            thermometerLED.material.color = ledOff;
        thermometerAnimating = false;
    }

    IEnumerator PrismCycle()
    {
        var rotation = 0f;
        while (true)
    	{
            var framerate = 1f / Time.deltaTime;
    		rotation += prismSpeed / framerate * (prismIsCcw ? -1 : 1);
    		prism.localEulerAngles = new Vector3(rotation, 0f, 0f);
    		yield return null;
    	}
    }

    IEnumerator ResetTemperature()
    {
        yield return new WaitForSeconds(15f);
        StartCoroutine(TemperatureChange(4));
    }

    IEnumerator SlowDownPrism()
    {
        var elapsed = 0f;
        var duration = 5f;
        while (elapsed < duration)
        {
            prismSpeed = Mathf.Lerp(20f, 0f, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        StopCoroutine(prismSpinning);
    }

    IEnumerator HideDot(Transform dot)
    {
        var elapsed = 0f;
        var duration = 1f;
        var start = dot.localPosition;
        while (elapsed < duration)
        {
            dot.localPosition = new Vector3(
                Mathf.Lerp(start.x, 0f, elapsed / duration),
                Mathf.Lerp(start.y, 0f, elapsed / duration),
                Mathf.Lerp(start.z, 0f, elapsed / duration)
            );
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    IEnumerator ResetEggs()
    {
        var elapsed = 0f;
        var duration = 1f;
        var start = new[] { topPivots[0].localRotation, topPivots[1].localRotation, bottomPivots[0].localRotation, bottomPivots[1].localRotation };
        var end = Quaternion.Euler(0f, 0f, 0f);
        while (elapsed < duration)
        {
            topPivots[0].localRotation = Quaternion.Slerp(start[0], end, elapsed / duration);
            topPivots[1].localRotation = Quaternion.Slerp(start[1], end, elapsed / duration);
            bottomPivots[0].localRotation = Quaternion.Slerp(start[2], end, elapsed / duration);
            bottomPivots[1].localRotation = Quaternion.Slerp(start[3], end, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    static int[] Swap(int[] start, int pos1, int pos2)
    {
        pos1--;
        pos2--;
        var digit1 = start[pos1];
        var digit2 = start[pos2];
        start[pos1] = digit2;
        start[pos2] = digit1;
        return start;
    }

    static char[] Swap(char[] start, int pos1, int pos2)
    {
        pos1--;
        pos2--;
        var char1 = start[pos1];
        var char2 = start[pos2];
        start[pos1] = char2;
        start[pos2] = char1;
        return start;
    }

    static int[] Shift(int[] start, int x)
    {
        var str = start.Join("");
        str = str.Substring(str.Length - x) + str.Substring(0, str.Length - x);
        for (int i = 0; i < start.Length; i++)
            start[i] = Int32.Parse(str[i].ToString());
        return start;
    }

    static char[] Shift(char[] start, int x)
    {
        var str = start.Join("");
        str = str.Substring(str.Length - x) + str.Substring(0, str.Length - x);
        return str.ToCharArray();
    }

    static int Process(int sq, DEMovement instruction)
    {
        int x = sq % 5, y = sq / 5;
        int x2 = x, y2 = y;
        switch (instruction)
        {
            case DEMovement.flipVertical: y = 4 - y; break;
            case DEMovement.flipHorizontal: x = 4 - x; break;
            case DEMovement.flipNESW: x = 4 - y2; y = 4 - x2; break;
            case DEMovement.flipNWSE: x = y2; y = x2; break;
            case DEMovement.rotateCW: y = x2; x = 4 - y2; break;
            case DEMovement.rotateCCW: y = 4 - x2; x = y2; break;
            case DEMovement.rotate180: x = 4 - x; y = 4 - y; break;
            default: break;
        }
        return (x % 5) + 5 * (y % 5);
    }

    // Twitch Plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} 1234 [Pushes the number buttons with those labels.] !{0} scrambled fried [Pressed the Scrambled cooking button and then the Fried cooking button. Valid cooking buttons are scrambled, fried, boiled, and sunny.]";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        input = input.ToLowerInvariant();
        if (input.Replace(" ", "").All(x => "1234".Contains(x)))
        {
            yield return null;
            input = input.Replace(" ", "");
            for (int i = 0; i < input.Length; i++)
            {
                numberButtons[Array.IndexOf(numberButtonLabels, Int32.Parse(input[i].ToString()))].OnInteract();
                while (thermometerAnimating)
                    yield return "trycancel";
                yield return new WaitForSeconds(1f);
            }
        }
        var inputArray = input.Split(' ').ToArray();
        var commands = new string[] { "sunny", "fried", "scrambled", "boiled" };
        if (inputArray.All(x => commands.Contains(x)))
        {
            if (!stage2)
            {
                yield return "sendtochaterror You can't do that right now.";
                yield break;
            }
            for (int i = 0; i < inputArray.Length; i++)
            {
                cookingButtons[Array.IndexOf(cookingButtonLabels, (DECooking) Array.IndexOf(commands, inputArray[i]))].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
        yield break;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (stage2)
            goto secondStage;
        while (!stage2)
        {
            numberButtons[Array.IndexOf(numberButtonLabels, stage1Solution[enteringStage])].OnInteract();
            while (thermometerAnimating)
                yield return true;
        }
        secondStage:
        while (!moduleSolved)
        {
            cookingButtons[Array.IndexOf(cookingButtonLabels, stage2Solution[enteringStage])].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }
}
