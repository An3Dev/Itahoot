public struct AnswerInfo
{
    public int actorNumber;
    public string answer;
    public float time;

    public AnswerInfo(int actorNumber, string answer, float time)
    {
        this.actorNumber = actorNumber;
        this.answer = answer;
        this.time = time;
    }
}
