"""
태스크 리포트 메인 스크립트
"""
import os
import logging
from core.github.handlers.project_handler import GitHubProjectHandler
from core.github.client import GitHubClient
from core.task.handlers.task_handler import TaskHandler
from core.task.formatters.report_formatter import ReportFormatter

# 로깅 설정
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s [%(levelname)s] %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)
logger = logging.getLogger(__name__)

# 외부 라이브러리 로깅 레벨 조정
logging.getLogger('urllib3').setLevel(logging.WARNING)
logging.getLogger('github').setLevel(logging.WARNING)

def main():
    try:
        # GitHub 토큰 확인
        github_token = os.environ.get('PAT') or os.environ.get('GITHUB_TOKEN')
        if not github_token:
            raise ValueError("GitHub 토큰이 설정되지 않았습니다.")
        
        # 저장소 정보 확인
        repo_name = os.environ.get('GITHUB_REPOSITORY')
        if not repo_name:
            raise ValueError("GitHub 저장소 정보를 찾을 수 없습니다.")
        
        project_name = repo_name.split('/')[-1]
        
        # GitHub 클라이언트 초기화
        github_client = GitHubClient(github_token)
        
        # TaskReport 생성 및 실행
        github_manager = GitHubProjectHandler(github_client)
        project_items = github_manager.get_project_items()
        task_issues = github_manager.get_task_issues()
        
        task_manager = TaskHandler(project_items, task_issues)
        report_formatter = ReportFormatter(project_name, task_manager)
        
        # 보고서 생성/업데이트
        github_manager.create_or_update_report(project_name, report_formatter)
        
    except Exception as e:
        logger.error(f"오류 발생: {str(e)}")
        logger.error(f"오류 상세: {type(e).__name__}")
        logger.error("스택 트레이스:", exc_info=True)
        raise

if __name__ == '__main__':
    main() 