import os
from github import Github
import logging
from collections import defaultdict

# 로깅 설정
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

def clean_duplicate_issues():
    try:
        # GitHub 연결
        github_token = os.environ.get("PAT") or os.environ["GITHUB_TOKEN"]
        g = Github(github_token)
        repo = g.get_repo(os.environ["GITHUB_REPOSITORY"])
        
        # 이슈 그룹화
        issue_groups = defaultdict(list)
        
        # 열린 이슈 가져오기
        logger.info("Fetching open issues...")
        open_issues = repo.get_issues(state='open')
        
        # 이슈 제목 기준으로 그룹화
        for issue in open_issues:
            # DSR 이슈는 제외
            if "Daily Development Log" in issue.title:
                continue
            
            normalized_title = issue.title.lower().strip()
            issue_groups[normalized_title].append(issue)
        
        # 중복 이슈 처리
        duplicates_found = False
        for title, issues in issue_groups.items():
            if len(issues) > 1:
                duplicates_found = True
                logger.info(f"\nFound duplicates for: {title}")
                
                # 가장 오래된 이슈를 원본으로 유지
                original_issue = min(issues, key=lambda x: x.created_at)
                logger.info(f"Keeping original issue #{original_issue.number} (created at {original_issue.created_at})")
                
                # 나머지 이슈 처리
                for issue in issues:
                    if issue.number != original_issue.number:
                        try:
                            # 중복 이슈에 코멘트 추가
                            issue.create_comment(f"Duplicate of #{original_issue.number}")
                            # 중복 이슈 닫기
                            issue.edit(state='closed')
                            logger.info(f"Closed duplicate issue #{issue.number}")
                            
                            # 원본 이슈에 참조 추가
                            original_issue.create_comment(f"Duplicate issue #{issue.number} has been closed")
                        except Exception as e:
                            logger.error(f"Error processing issue #{issue.number}: {str(e)}")
        
        if not duplicates_found:
            logger.info("No duplicate issues found")
            
    except Exception as e:
        logger.error(f"Error cleaning duplicate issues: {str(e)}")
        raise

if __name__ == "__main__":
    clean_duplicate_issues() 