using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Random = UnityEngine.Random;

public class SSubScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMBombInfo info;
    public GameObject[] cells;
    public KMSelectable[] buttons;
    public GameObject[] centers;
    public Renderer[] brends;
    public Material[] mats;
    public GameObject endcell;
    public Renderer rendcell;
    public GameObject matstore;

    private readonly int[,,] grids = new int[5, 4, 4]
    {
        { {2, 1, 3, 0}, {3, 0, 2, 1}, {1, 2, 0, 3}, {0, 3, 1, 2} },
        { {0, 0, 2, 3}, {1, 2, 1, 3}, {3, 1, 2, 1}, {3, 2, 0, 0} },
        { {1, 1, 1, 2}, {0, 0, 2, 3}, {1, 2, 0, 0}, {2, 3, 3, 3} },
        { {3, 3, 0, 0}, {3, 1, 2, 0}, {2, 0, 3, 1}, {2, 2, 1, 1} },
        { {2, 1, 2, 3}, {3, 0, 1, 0}, {0, 1, 0, 3}, {3, 2, 1, 2} }
    };
    private int gridselect;
    private int initial;
    private bool[][] split = new bool[5][] { new bool[4], new bool[4], new bool[4], new bool[4], new bool[4] };
    private int[,] arrange = new int[21, 4];
    private int[] firstarrange = new int[4];
    private List<int>[] sequences = new List<int>[3] { new List<int> { }, new List<int> { }, new List<int> { } };
    private List<int>[] flashseq = new List<int>[2] { new List<int> { }, new List<int> { } };
    private string[] logseq = new string[3];
    private int subnum;
    private int step;
    private IEnumerator flash;
    private bool seq;
    private bool subdiv;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        endcell.SetActive(false);
        matstore.SetActive(false);
        for(int i = 0; i < 21; i++)
        {
            int[] r = new int[4] { 0, 1, 2, 3 }.Shuffle();
            for (int j = 0; j < 4; j++)
            {
                arrange[i, j] = r[j];
                if (i == 0)
                    firstarrange[j] = r[j];
            }
        }
        foreach(KMSelectable button in buttons)
        {
            int b = Array.IndexOf(buttons, button);
            button.OnInteract += delegate () { Press(b); return false; };
        }
        for (int i = 0; i < 4; i++)
            brends[i].material = mats[arrange[0, i]];
        module.OnActivate = Newseq;
        module.OnActivate += delegate () { Disappear(); } ;
    }

    private void Disappear()
    {
        for (int i = 4; i < 84; i++)
            cells[i].SetActive(false);
    }

    private void Newseq()
    {
        for (int i = 0; i < 3; i++)
        {
            sequences[i].Clear();
            logseq[i] = "";
        }
        for (int i = 0; i < 2; i++)
            flashseq[i].Clear();
        if (subnum == 0)
        {
            for (int i = 0; i < 7; i++)
            {
                if (i < 6 && "RBVG".Contains(info.GetSerialNumber()[i]))
                {
                    sequences[0].Add("RBVG".IndexOf(info.GetSerialNumber()[i]));
                    break;
                }
                else if (i == 6)
                    sequences[0].Add(arrange[0, 0]);
            }
            gridselect = info.GetSerialNumberNumbers().Last();
            Debug.LogFormat("[Simon Subdivides #{0}] The initial colour is {1}", moduleID, "RBVG"[sequences[0][0]]);
        }
        else if (subnum % 2 == 0)
            sequences[0].Add(firstarrange[(Array.IndexOf(firstarrange, initial) + 1) % 4]);
        else
            sequences[0].Add(initial);
        initial = sequences[0][0];
        int j = Random.Range(3, 6);
        for (int i = 0; i < j; i++)
        {
            int r = Random.Range(0, 4);
            logseq[0] += "URDL"[r].ToString();
            logseq[1] += "RBVG"[arrange[0, r]].ToString();
            sequences[0].Add(arrange[0, r]);
            sequences[1].Add(grids[(gridselect + subnum) % 5, sequences[0][i], sequences[0][i + 1]]);
            if (split[0][r])
            {
                int s = Random.Range(0, 4);
                int t = ((r + 1) * 4) + s;
                logseq[0] += "URDL"[s].ToString();
                logseq[1] += "RBVG"[arrange[r + 1, s]].ToString();
                sequences[0].Add(arrange[r + 1, s]);
                sequences[1].Add(grids[(gridselect + subnum) % 5, sequences[0][i], sequences[0][i + 2]]);
                if (split[r + 1][s])
                {
                    int u = Random.Range(0, 4);
                    sequences[0].Add(arrange[t + 1, u]);
                    sequences[1].Add(grids[(gridselect + subnum) % 5, sequences[0][i], sequences[0][i + 3]]);
                    flashseq[0].Add(((t + 1) * 4) + u);
                    flashseq[1].Add(arrange[t + 1, u]);
                    logseq[0] += "URDL"[u].ToString();
                    logseq[1] += "RBVG"[arrange[t + 1, u]].ToString();
                    i += 2;
                }
                else
                {
                    flashseq[0].Add(t);
                    flashseq[1].Add(arrange[r + 1, s]);
                    i++;
                }
            }
            else
            {
                flashseq[0].Add(r);
                flashseq[1].Add(arrange[0, r]);
            }
            if (i < j - 1)
            {
                logseq[0] += ", ";
                logseq[1] += ", ";
            }
        }
        int k = sequences[1].Count();
        for (int i = 0; i < k; i++)
        {
            int[] r = new int[3];
            r[0] = sequences[1][i];
            logseq[2] += "URDL"[r[0]];
            if (split[0][r[0]])
            {
                i++;
                r[1] = ((r[0] + 1) * 4) + sequences[1][i % k];
                logseq[2] += "URDL"[sequences[1][i % k]];
                if (split[r[0] + 1][sequences[1][i % k]])
                {
                    i++;
                    r[2] = ((r[1] + 1) * 4) + sequences[1][i % k];
                    logseq[2] += "URDL"[sequences[1][i % k]];
                    sequences[2].Add(r[2]);
                }
                else
                    sequences[2].Add(r[1]);
            }
            else
                sequences[2].Add(r[0]);
            if (i < sequences[1].Count() - 1)
                logseq[2] += ", ";
        }
        flash = Flash(flashseq[0].ToArray(), flashseq[1].ToArray());
        seq = true;
        StartCoroutine(flash);
        Debug.LogFormat("[Simon Subdivides #{0}] The positions of the flashing cells are: {1}", moduleID, logseq[0]);
        Debug.LogFormat("[Simon Subdivides #{0}] The colours of the flashing cells are: {1}", moduleID, logseq[1]);
        Debug.LogFormat("[Simon Subdivides #{0}] The sequence of expected inputs, obtained from grid {2}, are: {1}", moduleID, logseq[2], (gridselect + subnum) % 5);
    }

    private IEnumerator Flash(int[] p, int[] c)
    {
        for(int i = 0; i < p.Length; i++)
        {
            brends[p[i]].material = mats[c[i] + 4];
            yield return new WaitForSeconds(0.4f);
            brends[p[i]].material = mats[c[i]];
            yield return new WaitForSeconds(0.4f);
            if(i == p.Length - 1)
            {
                yield return new WaitForSeconds(0.8f);
                i = -1;
            }
        }
    }

    private void Press(int b)
    {
        if (!moduleSolved && !subdiv)
        {
            buttons[b].AddInteractionPunch();
            if (seq)
            {
                seq = false;
                for (int i = 0; i < 84; i++)
                    if (cells[i].activeSelf)
                        brends[i].material = mats[arrange[i / 4, i % 4]];
                StopCoroutine(flash);
            }
            if(b == sequences[2][step])
                step++;
            else
            {
                step = 0;
                module.HandleStrike();
                Newseq();
            }
            if(step != sequences[2].Count())
                StartCoroutine(Select(b));
            else
            {
                int s = sequences[2].Last();
                if (s > 19)
                {
                    moduleSolved = true;
                    module.HandlePass();
                    StartCoroutine(Solve(arrange[s / 4, s % 4] + 4));
                }
                else
                {
                    subdiv = true;
                    split[s / 4][s % 4] = true;
                    StartCoroutine(Subdivide(s));
                }
            }
        }
    }
    
    private IEnumerator Select(int b)
    {
        Audio.PlaySoundAtTransform("Cell" + (arrange[b / 4, b % 4] + 1).ToString(), cells[b].transform);
        brends[b].material = mats[arrange[b / 4, b % 4] + 4];
        yield return new WaitForSeconds(0.2f);
        if(!moduleSolved)
            brends[b].material = mats[arrange[b / 4, b % 4]];
    }

    private IEnumerator Subdivide(int s)
    {
        Audio.PlaySoundAtTransform("Split", cells[s].transform);
        brends[s].material = mats[arrange[s / 4, s % 4] + 4];
        int sub = (s + 1) * 4;
        for (int i = 0; i < 4; i++)
        {
            cells[sub + i].SetActive(true);
            brends[sub + i].material = mats[arrange[s / 4, s % 4] + 4];
        }
        for (int i = 0; i < 10; i++)
        {
            cells[s].transform.localScale *= 0.75f;
            if (s < 4)
            {
                cells[sub].transform.localPosition += new Vector3(0, 0, 0.001f);
                cells[sub + 1].transform.localPosition += new Vector3(0.001f, 0, 0);
                cells[sub + 2].transform.localPosition -= new Vector3(0, 0, 0.001f);
                cells[sub + 3].transform.localPosition -= new Vector3(0.001f, 0, 0);
            }
            else
            {
                cells[sub].transform.localPosition += new Vector3(0, 0, 0.002f);
                cells[sub + 1].transform.localPosition += new Vector3(0.002f, 0, 0);
                cells[sub + 2].transform.localPosition -= new Vector3(0, 0, 0.002f);
                cells[sub + 3].transform.localPosition -= new Vector3(0.002f, 0, 0);
            }
            yield return new WaitForSeconds(0.1f);
        }
        cells[s].SetActive(false);
        for(int i = 0; i < 4; i++)
        {
            brends[sub + i].material = mats[arrange[sub / 4, (sub + i) % 4]];
            if (s < 4)
                centers[sub + i].transform.localPosition = new Vector3(cells[sub + i].transform.localPosition.x, centers[sub + i].transform.localPosition.y, cells[sub + i].transform.localPosition.z);
        }
        step = 0;
        subnum++;
        subdiv = false;
        Newseq();
    }

    private IEnumerator Solve(int c)
    {
        Audio.PlaySoundAtTransform("Solve", transform);
        int[,] ends = new int[104, 2];
        Vector3[] p = new Vector3[84];
        Vector3[] n = new Vector3[20];
        for (int i = 0; i < 20; i++)
        {
            n[i] = centers[i].transform.localPosition;
            ends[i + 84, 0] = Random.Range(0, 15);
            ends[i + 84, 1] = Random.Range(ends[i, 0] + 10, 30);
        }
        for (int i = 0; i < 84; i++)
        {
            if (cells[i].activeSelf)
                brends[i].material = mats[c];
            ends[i, 0] = Random.Range(0, 15);
            ends[i, 1] = Random.Range(ends[i, 0] + 10, 30);
            p[i] = cells[i].transform.localPosition;
        }
        for(int i = 0; i < 30; i++)
        {
            for(int j = 0; j < 84; j++)
                if(i >= ends[j, 0] && i <= ends[j, 1])
                    cells[j].transform.localPosition = new Vector3(Mathf.Lerp(p[j].x, 0, (float)(i - ends[j, 0]) / (ends[j, 1] - ends[j, 0])), p[j].y, Mathf.Lerp(p[j].z, 0, (float)(i - ends[j, 0]) / (ends[j, 1] - ends[j, 0])));
            for (int j = 0; j < 20; j++)
                if (i >= ends[j + 84, 0] && i <= ends[j + 84, 1])
                    centers[j].transform.localPosition = new Vector3(Mathf.Lerp(n[j].x, 0, (float)(i - ends[j, 0]) / (ends[j, 1] - ends[j, 0])), n[j].y, Mathf.Lerp(n[j].z, 0, (float)(i - ends[j, 0]) / (ends[j, 1] - ends[j, 0])));
            if(i == 14)
            {
                endcell.SetActive(true);
                rendcell.material = mats[c];
            }
            if(i > 14)
                endcell.transform.localScale = new Vector3(Mathf.Lerp(0.01f, 0.1f, (float)(i - 15) / 15), 0.01f, Mathf.Lerp(0.01f, 0.1f, (float)(i - 15) / 15));
            yield return new WaitForSeconds(0.1f);
        }
        for (int i = 0; i < 84; i++)
            cells[i].SetActive(false);
        rendcell.material = mats[c - 4];
    }
#pragma warning disable 414
    private string TwitchHelpMessage = "!{0} <URDL> [Selects cell in the specified direction. Chain directions without spaces to select within subdivided cells. Chain inputs with spaces to select multiple cells.]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToUpperInvariant();
        if(!command.All(d => "URDL ".Contains(d)))
        {
            yield return "sendtochaterror!f Invalid character in command";
            yield break;
        }
        string[] commands = command.Split(' ');
        int[] s = new int[commands.Length];
        for(int i = 0; i < commands.Length; i++)
        {
            if(commands[i].Length > 3)
            {
                yield return "sendtochaterror!f Cell" + commands[i] + " does not exist.";
            }
            int[] r = new int[3];
            r[0] = "URDL".IndexOf(commands[i][0]);
            if (!split[0][r[0]] && commands[i].Length > 1)
            {
                yield return "sendtochaterror!f Cell " + commands[i][0] + " has not divided.";
                yield break;
            }
            if (split[0][r[0]])
            {
                if (commands[i].Length == 1)
                {
                    yield return "sendtochaterror!f Cell " + commands[i][0] + " has divided. Add another direction to select a daughter cell.";
                    yield break;
                }
                else
                {
                    r[1] = ((r[0] + 1) * 4) + "URDL".IndexOf(commands[i][1]);
                    if (!split[r[0] + 1]["URDL".IndexOf(commands[i][1])] && commands[i].Length > 2)
                    {
                        yield return "sendtochaterror!f Cell " + commands[i][0] + commands[i][1] + " has not divided.";
                        yield break;
                    }
                    if (split[r[0] + 1]["URDL".IndexOf(commands[i][1])])
                    {
                        if (commands[i].Length == 2)
                        {
                            yield return "sendtochaterror!f Cell " + commands[i][0] + commands[i][1] + " has divided. Add another direction to select a daughter cell.";
                            yield break;
                        }
                        else
                        {
                            r[2] = ((r[1] + 1) * 4) + "URDL".IndexOf(commands[i][2]);
                            s[i] = r[2];
                        }
                    }
                    else
                        s[i] = r[1];
                }
            }
            else
                s[i] = r[0];
        }
        for(int i = 0; i < s.Length; i++)
        {
            yield return null;
            buttons[s[i]].OnInteract();
            yield return new WaitForSeconds(0.15f);
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            yield return null;
            buttons[sequences[2][step]].OnInteract();
            yield return new WaitForSeconds(0.15f);
            while (subdiv)
                yield return true;
        }
    }
}
