"""
사용자 매핑 정보를 관리하는 설정 파일
"""

GITHUB_USER_MAPPING = {
    "Anxi77": {
        "name": "최현성",
        "role": "개발팀 팀장",
        "branch_suffix": "HSChoi",
        "slack_id": "@최현성",
        "position": "head_developer"
    },
    "beooom": {
        "name": "김범희",
        "role": "백엔드/컨텐츠 개발",
        "branch_suffix": "KimBeom",
        "slack_id": "@김범희",
        "position": "developer"
    },
    "Jine99": {
        "name": "김진",
        "role": "컨텐츠 개발",
        "branch_suffix": "JKim",
        "slack_id": "@김진",
        "position": "developer"
    },
    "hyeonji9178": {
        "name": "김현지",
        "role": "컨텐츠 개발",
        "branch_suffix": "HJKim",
        "slack_id": "@김현지",
        "position": "developer"
    },
    "Rjcode7387": {
        "name": "류지형",
        "role": "컨텐츠 개발", 
        "branch_suffix": "JHRYU",
        "slack_id": "@류지형",
        "position": "developer"
    },
    "project_manager": {
        "name": "이은영",
        "role": "PM",
        "slack_id": "@이은영",
        "position": "pm"
    }
}

def get_slack_users_by_position(position: str) -> list:
    """특정 포지션의 Slack 사용자 ID 목록을 반환합니다."""
    return [
        user["slack_id"] 
        for user in GITHUB_USER_MAPPING.values() 
        if user.get("position") == position
    ]

def get_user_info(github_username: str) -> dict:
    """GitHub 사용자명으로 사용자 정보를 조회합니다."""
    return GITHUB_USER_MAPPING.get(github_username, {})

def get_branch_name(github_username: str) -> str:
    """사용자의 개발 브랜치명을 생성합니다."""
    user_info = get_user_info(github_username)
    branch_suffix = user_info.get("branch_suffix", github_username)
    return f"Dev_{branch_suffix}" 