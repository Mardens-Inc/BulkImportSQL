namespace BulkImportSQL.cli;

public class Progressbar
{
    public static void Draw(int processed, int total)
    {
        Console.Clear();
        Console.CursorVisible = false;
        Console.CursorLeft = 0;
        Console.Write("[");
        Console.CursorLeft = Console.WindowWidth - 1;
        Console.Write("]");
        Console.CursorLeft = 1;
        float oneChunk = (Console.WindowWidth - 3) / (float)total;
        int position = 1;
        for (int i = 0; i < oneChunk * processed; i++)
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.CursorLeft = position++;
            Console.Write(" ");
        }

        Console.BackgroundColor = ConsoleColor.Black;
        Console.CursorLeft = Console.WindowWidth - 2;
        Console.Write(" ");

        Console.CursorLeft = Console.WindowWidth + 1;
        Console.Write($"{processed}/{total} - {((float)processed / total):P2}");

        Console.CursorVisible = true;
    }
}