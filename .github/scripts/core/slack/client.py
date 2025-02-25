"""Slack API 클라이언트"""
import os
from typing import Dict, List
from slack_sdk import WebClient
from slack_sdk.errors import SlackApiError
from config.user_mappings import get_slack_users_by_position

class SlackClient:
    def __init__(self, token: str):
        self.client = WebClient(token=token)
        self.channel_id = os.environ['SLACK_CHANNEL_ID']
        self.pm_id = get_slack_users_by_position('pm')[0].replace('@', '')
        self.head_dev_id = get_slack_users_by_position('head_developer')[0].replace('@', '')
    
    def send_channel_notification(self, message: Dict):
        """채널 알림 전송"""
        try:
            self.client.chat_postMessage(
                channel=self.channel_id,
                blocks=message['blocks'],
                text=message['blocks'][0]['text']['text']
            )
        except SlackApiError as e:
            print(f"채널 메시지 전송 실패: {e.response['error']}")
    
    def send_pm_report(self, message: Dict):
        """PM과 헤드 개발자에게 리포트 전송"""
        recipients = [self.pm_id, self.head_dev_id]
        
        for recipient in recipients:
            try:
                self.client.chat_postMessage(
                    channel=recipient,
                    blocks=message['blocks'],
                    text=message['blocks'][0]['text']['text']
                )
            except SlackApiError as e:
                print(f"리포트 전송 실패 (수신자: {recipient}): {e.response['error']}")
    
    def send_dm(self, user_id: str, blocks: List[Dict], text: str):
        """DM 전송"""
        try:
            self.client.chat_postMessage(
                channel=user_id.replace('@', ''),
                blocks=blocks,
                text=text
            )
        except SlackApiError as e:
            print(f"DM 전송 실패 ({user_id}): {e.response['error']}") 