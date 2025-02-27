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
            # '@' 기호 제거 및 사용자 ID 정리
            clean_user_id = user_id.replace('@', '').strip()
            
            # 사용자 ID가 이메일 형식인지 확인
            if '@' in clean_user_id and '.' in clean_user_id:
                # 이메일 형식이면 사용자 조회
                response = self.client.users_lookupByEmail(email=clean_user_id)
                if response["ok"]:
                    clean_user_id = response["user"]["id"]
            
            # 사용자 ID로 DM 채널 열기
            response = self.client.conversations_open(users=[clean_user_id])
            if response["ok"]:
                channel_id = response["channel"]["id"]
                
                # DM 채널에 메시지 전송
                self.client.chat_postMessage(
                    channel=channel_id,
                    blocks=blocks,
                    text=text
                )
                print(f"DM 전송 성공 ({user_id})")
            else:
                print(f"DM 채널 열기 실패 ({user_id}): {response.get('error', '알 수 없는 오류')}")
                
        except SlackApiError as e:
            print(f"DM 전송 실패 ({user_id}): {e.response['error']}") 