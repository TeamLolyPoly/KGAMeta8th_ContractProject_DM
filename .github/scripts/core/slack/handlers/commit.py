"""커밋 이벤트 핸들러"""
from typing import Dict
from .base import BaseHandler
from ..formatters.commit import CommitFormatter

class CommitHandler(BaseHandler):
    """커밋 이벤트 처리"""
    
    def handle(self, event_data: Dict):
        """커밋 이벤트 처리"""
        repository = event_data['repository']['full_name']
        branch = event_data['ref'].split('/')[-1]
        commits = event_data['commits']
        
        if commits:
            message = CommitFormatter.format_commits(commits, repository, branch)
            self.client.send_channel_notification(message) 