import os
import json
import logging
from github import Github

# 로깅 설정
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger(__name__)

def update_project_board():
    """메인 함수 개선"""
    try:
        logger.info("Starting project board update")
        github_token = os.environ.get("PAT") or os.environ["GITHUB_TOKEN"]
        g = Github(github_token)
        
        with open(os.environ["GITHUB_EVENT_PATH"]) as f:
            event = json.load(f)
        
        repo = g.get_repo(os.environ["GITHUB_REPOSITORY"])
        
        # 프로젝트 URL로 직접 접근
        project = find_project(repo, None, g)
        
        if not project:
            logger.error("Could not find project")
            return
            
        columns = {
            "To Do": get_or_create_column(project, "To Do"),
            "In Progress": get_or_create_column(project, "In Progress"),
            "Done": get_or_create_column(project, "Done")
        }
        
        event_type = os.environ["GITHUB_EVENT_NAME"]
        
        if event_type == "push":
            for commit in event.get("commits", []):
                handle_commit_todos(commit, columns, repo)
                
    except Exception as e:
        logger.error(f"Failed to update project board: {str(e)}")
        raise

def handle_commit_todos(commit, columns, repo):
    logger.info(f"Processing TODOs from commit: {commit['id']}")
    message = commit["message"]
    todo_section = ""
    is_todo = False
    
    # 커밋 메시지 파싱 로직 개선
    lines = message.split("\n")
    in_todo_section = False
    current_category = None
    
    for line in lines:
        line = line.strip()
        
        # [TODO] 섹션 시작
        if line.lower() == "[todo]":
            in_todo_section = True
            continue
            
        # 다른 섹션 시작되면 TODO 섹션 종료
        if line.lower() in ["[body]", "[footer]"]:
            in_todo_section = False
            continue
            
        if in_todo_section:
            # 카테고리 처리
            if line.startswith("@"):
                current_category = line[1:].strip()
                continue
                
            # TODO 항목 처리
            if line.startswith(("-", "*")):
                todo_text = line[1:].strip()
                
                # (issue) 태그 처리
                if todo_text.startswith("(issue)"):
                    todo_text = todo_text[7:].strip()
                    try:
                        # 이슈 생성
                        issue = repo.create_issue(
                            title=todo_text,
                            body=f"""Created from commit {commit['id'][:7]}
                            
Category: {current_category or 'General'}
Original TODO item: {todo_text}""",
                            labels=["todo", f"category:{current_category}" if current_category else None]
                        )
                        
                        # 프로젝트 카드 생성
                        card = columns["To Do"].create_card(
                            content_id=issue.id,
                            content_type="Issue"
                        )
                        
                        logger.info(f"Created issue #{issue.number} and added to project board")
                        
                    except Exception as e:
                        logger.error(f"Failed to create issue for TODO: {todo_text}")
                        logger.error(f"Error: {str(e)}")

def find_project(repo, project_name, github_obj):
    """프로젝트 검색 로직 개선"""
    logger.info(f"Looking for project: {project_name}")
    
    # 프로젝트 URL로 직접 접근
    project_url = "https://github.com/orgs/KGAMeta8thTeam1/projects/2"
    project_number = int(project_url.split('/')[-1])  # 2
    org_name = project_url.split('/')[4]  # KGAMeta8thTeam1
    
    try:
        logger.info(f"Trying to access project directly via URL: {project_url}")
        org = github_obj.get_organization(org_name)
        project = org.get_project(project_number)
        if project:
            logger.info(f"Found project: {project.name}")
            return project
    except Exception as e:
        logger.warning(f"Error accessing project via URL: {str(e)}")
        
    # 기존 검색 로직은 폴백으로 유지
    try:
        logger.info("Falling back to organization project search...")
        org = github_obj.get_organization(org_name)
        org_projects = list(org.get_projects(state='open'))
        logger.debug(f"Found {len(org_projects)} organization projects")
        for project in org_projects:
            logger.debug(f"Found project: {project.name}")
            if project.number == project_number:
                logger.info(f"Found matching project: {project.name}")
                return project
    except Exception as e:
        logger.warning(f"Error in fallback search: {str(e)}")

    logger.warning("Project not found")
    return None

def get_or_create_column(project, name):
    """칼럼 가져오기 또는 생성"""
    logger.debug(f"Looking for column: {name}")
    for column in project.get_columns():
        if column.name == name:
            logger.debug(f"Found existing column: {name}")
            return column
    logger.info(f"Creating new column: {name}")
    return project.create_column(name)

def handle_card_movement(card_event, columns, repo):
    """카드 이동 처리"""
    logger.info(f"Processing card movement event: {card_event['id']}")
    if card_event["column_id"]:
        logger.debug(f"Card moved to column ID: {card_event['column_id']}")
        issue = get_issue_from_card(card_event, repo)
        if issue:
            logger.info(f"Updating status for issue #{issue.number}")
            update_issue_status(issue, card_event["column_id"], columns)
        else:
            logger.debug("No associated issue found for card")

def handle_issue_status(issue, columns):
    """이슈 상태 변경 처리"""
    logger.info(f"Processing issue status: #{issue['number']}")
    if any(label["name"] == "done" for label in issue["labels"]):
        logger.info("Moving issue to Done column")
        move_to_column(issue, columns["Done"])
    elif any(label["name"] == "in-progress" for label in issue["labels"]):
        logger.info("Moving issue to In Progress column")
        move_to_column(issue, columns["In Progress"])
    else:
        logger.info("Moving issue to To Do column")
        move_to_column(issue, columns["To Do"])

def get_issue_from_card(card, repo):
    """카드에서 이슈 정보 추출"""
    logger.debug(f"Extracting issue from card: {card['id']}")
    if "content_url" in card:
        issue_url = card["content_url"]
        issue_number = int(issue_url.split("/")[-1])
        logger.debug(f"Found issue #{issue_number}")
        return repo.get_issue(issue_number)
    logger.debug("No content URL found in card")
    return None

def move_to_column(issue, column):
    """이슈를 지정된 칼럼으로 이동"""
    logger.info(f"Moving issue #{issue.number} to column: {column.name}")
    for card in column.get_cards():
        if card.content_url == issue.url:
            logger.debug("Issue already in target column")
            return
    column.create_card(content_id=issue.id, content_type="Issue")
    logger.info(f"Created new card for issue #{issue.number} in column {column.name}")

def update_issue_status(issue, column_id, columns):
    """이슈 상태 업데이트"""
    logger.info(f"Updating status for issue #{issue.number}")
    status_labels = {
        columns["To Do"].id: "todo",
        columns["In Progress"].id: "in-progress",
        columns["Done"].id: "done"
    }
    
    if column_id in status_labels:
        logger.debug(f"Updating labels for column ID: {column_id}")
        for label in issue.labels:
            if label.name in status_labels.values():
                logger.debug(f"Removing label: {label.name}")
                issue.remove_from_labels(label.name)
        
        new_label = status_labels[column_id]
        logger.debug(f"Adding new label: {new_label}")
        issue.add_to_labels(new_label)
    else:
        logger.warning(f"Unknown column ID: {column_id}")

if __name__ == "__main__":
    update_project_board() 