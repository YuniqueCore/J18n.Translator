namespace J18n.Test.TestData.Json;

public static class CommonTestData
{
    public class ParsePathToKeysData
    {
        public string JointPath { get; set; }
        public string[] Keys { get; set; }
    }
    public static IEnumerable<ParsePathToKeysData> parsePathToKeysDatas =
        [
            new()
            {
                JointPath = ".abc[0].hello.ccc.abc" ,
                Keys = ["abc" , "[0]" , "hello" , "ccc" , "abc"]
            } ,
            new()
            {
                JointPath = ".[0][0].NestObject[0].Object.Array[0][2]" ,
                Keys = ["[0]" , "[0]" , "NestObject" , "[0]" , "Object" , "Array" , "[0]" , "[2]"]
            } ,
            new()
            {
                JointPath = ".[0].[0].NestObject.Object.Array[0][2]" ,
                Keys = ["[0]" , "[0]" , "NestObject" , "Object" , "Array" , "[0]" , "[2]"]
            }
        ];

}