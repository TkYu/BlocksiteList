# GreatFireList
This is a [greatfire](https://en.greatfire.org/analyzer) clawer

## About
This just a little tiny work, i clawed this site and take these domain to an [AutoProxy file](https://github.com/TkYu/BlocksiteList/blob/master/opt/greatfirelist.txt) (where Censored percent >= 50%).
BTW i drop these chinese uri to Elasticsearch(with ik) to analysis ["key words"](https://github.com/TkYu/BlocksiteList/blob/master/opt/BlackWords.txt)

## If u want run it by yourself
1.  Install [.net core](https://www.microsoft.com/net/core)
2.  Install [elasticsearch](https://www.elastic.co/downloads)
3.  Install [elasticsearch-ik](https://github.com/medcl/elasticsearch-analysis-ik)
4.  Check out this project and modify Program.cs/elastic to yours
5.  dotnet restore & dotnet run