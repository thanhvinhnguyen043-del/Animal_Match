using System;
using System.Collections.Generic;
using UnityEngine;

public enum LevelLayoutMode { Automatic, Manual }

[Serializable]
public class LevelCell
{
    public bool empty;
    public int animalID;
    public TileState state;
}

[Serializable]
public class LevelDefinition
{
    public string name = "Level 1";
    public LevelLayoutMode mode;
    [Range(2, 20)] public int columns = 10;
    [Range(2, 20)] public int rows = 5;
    public float tileSize = 110;
    public float timeLimit = 60;
    public float timeBonus = 2;
    public GravityDirection gravity = GravityDirection.Down;
    [Min(1)] public int animalKinds = 6;
    public int seed = 1;
    public bool randomEachPlay = true;
    [Range(0, 1)] public float iceChance;
    [Range(0, 1)] public float barrierChance;

    public List<LevelCell> cells = new List<LevelCell>();

    public List<LevelCell> Generate(int spriteCount, int generationSeed)
    {
        var random = new System.Random(generationSeed);
        var result = new List<LevelCell>();
        int kinds = Mathf.Clamp(animalKinds, 1, spriteCount);
        for (int i = 0; i < columns * rows / 2; i++)
        {
            int id = random.Next(kinds);
            double roll = random.NextDouble();
            TileState state = roll < iceChance ? TileState.Ice :
                roll < iceChance + barrierChance ? TileState.Barrier : TileState.Normal;
            result.Add(new LevelCell { animalID = id, state = state });
            result.Add(new LevelCell { animalID = id, state = state });
        }

        if (result.Count >= 2 && result.TrueForAll(cell => cell.state == TileState.Barrier))
        { result[0].state = TileState.Normal; result[1].state = TileState.Normal; }
        for (int i = result.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            LevelCell temp = result[i]; result[i] = result[j]; result[j] = temp;
        }
        return result;
    }

    public bool Validate(int spriteCount, out string error)
    {
        error = null;
        if (!Enum.IsDefined(typeof(LevelLayoutMode), mode) || !Enum.IsDefined(typeof(GravityDirection), gravity))
            error = "Che do hoac trong luc khong hop le.";
        else if (columns < 2 || columns > 20 || rows < 2 || rows > 20 ||
            tileSize <= 0 || timeLimit <= 0 || timeBonus < 0)
            error = "Kich thuoc 2..20, co o/thoi gian > 0, thuong thoi gian >= 0.";
        else if (spriteCount < 1) error = "Database chua co hinh thu.";
        else if (mode == LevelLayoutMode.Automatic && ((columns * rows) % 2 != 0 ||
            animalKinds < 1 || animalKinds > spriteCount || iceChance < 0 || barrierChance < 0 ||
            iceChance + barrierChance > 1 || barrierChance >= 1))
            error = "Tu dong: so o phai chan, so loai thu hop le, tong ti le <= 1 va chan < 100%.";
        else if (mode == LevelLayoutMode.Manual)
        {
            if (cells == null || cells.Count != columns * rows)
                error = "Bo cuc chua khop kich thuoc. Bam Tao/doi kich thuoc ban.";
            else
            {
                var counts = new Dictionary<int, int>();
                int occupied = 0, selectable = 0;
                foreach (LevelCell cell in cells)
                {
                    if (cell == null) { error = "Co o du lieu rong."; break; }
                    if (cell.empty) continue;
                    if (cell.animalID < 0 || cell.animalID >= spriteCount || cell.state == TileState.Hidden ||
                        !Enum.IsDefined(typeof(TileState), cell.state))
                    { error = "ID thu khong hop le hoac trang thai Hidden khong duoc ho tro."; break; }
                    occupied++;
                    if (cell.state != TileState.Barrier) selectable++;
                    counts.TryGetValue(cell.animalID, out int count);
                    counts[cell.animalID] = count + 1;
                }
                if (error == null && (occupied == 0 || selectable < 2))
                    error = "Can it nhat 2 o thu co the chon.";
                foreach (var pair in counts)
                    if (error == null && pair.Value % 2 != 0)
                        error = "Thu ID " + pair.Key + " co so luong le. Hay xep thanh cap.";
            }
        }
        return error == null;
    }
}

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Game/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public Sprite[] animalSprites = new Sprite[0];
    public List<LevelDefinition> levels = new List<LevelDefinition> { new LevelDefinition() };
}
