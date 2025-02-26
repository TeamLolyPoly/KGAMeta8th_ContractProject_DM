"""
GitHub 커밋 알림
"""
import os
import json
from core.slack.client import SlackClient
from core.slack.handlers.commit import CommitHandler

def load_event_data():
    """GitHub 이벤트 데이터 로드"""
    with open(os.environ['GITHUB_EVENT_PATH'], 'r', encoding='utf-8') as f:
        return json.load(f)

def main():
    """커밋 알림 실행"""
    event_data = load_event_data()
    
    slack_token = os.environ['SLACK_BOT_TOKEN']
    client = SlackClient(slack_token)
    
    handler = CommitHandler(client)
    handler.handle(event_data)

if __name__ == '__main__':
    main() 