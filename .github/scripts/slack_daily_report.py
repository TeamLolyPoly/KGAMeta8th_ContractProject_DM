"""
일일 태스크 리포트 전송
"""
import os
from core.slack.client import SlackClient
from core.slack.handlers.report import ReportHandler
from core.github.handlers.project_handler import GitHubProjectHandler
from core.github.client import GitHubClient

def main():
    """일일 리포트 실행"""
    slack_token = os.environ['SLACK_BOT_TOKEN']
    github_token = os.environ.get('GITHUB_TOKEN')
    
    client = SlackClient(slack_token)
    github_client = GitHubClient(github_token)
    github_manager = GitHubProjectHandler(github_client)
    handler = ReportHandler(client, github_manager)
    handler.handle()

if __name__ == '__main__':
    main() 