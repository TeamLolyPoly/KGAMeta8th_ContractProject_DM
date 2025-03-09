"""
태스크 관련 Slack 알림
"""
import os
import json
from core.slack.client import SlackClient
from core.slack.handlers.task import TaskHandler
from core.slack.handlers.proposal import ProposalHandler

def load_event_data():
    """GitHub 이벤트 데이터 로드"""
    with open(os.environ['GITHUB_EVENT_PATH'], 'r', encoding='utf-8') as f:
        return json.load(f)

def is_proposal_event(event_data):
    """제안서 이벤트 여부 확인"""
    if 'issue' not in event_data:
        return False
    labels = [label['name'] for label in event_data['issue'].get('labels', [])]
    return any(label in labels for label in ['⌛ 검토대기', '✅ 승인완료', '❌ 반려', '⏸️ 보류'])

def main():
    """태스크 알림 실행"""
    event_data = load_event_data()
    
    slack_token = os.environ['SLACK_BOT_TOKEN']
    client = SlackClient(slack_token)
    
    if is_proposal_event(event_data):
        handler = ProposalHandler(client)
    else:
        handler = TaskHandler(client)
    
    handler.handle(event_data)

if __name__ == '__main__':
    main() 