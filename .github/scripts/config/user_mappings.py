"""
사용자 매핑 정보를 관리하는 설정 파일
"""
GITHUB_USER_MAPPING = {
    "Anxi77": {
        "name": "최현성",
        "role": "개발팀 팀장",
        "branch_suffix": "HSChoi",
        "slack_id": "U08EHHU46LR",
        "position": "head_developer"
    },
    "beooom": {
        "name": "김범희",
        "role": "백엔드/컨텐츠 개발",
        "branch_suffix": "KimBeom",
        "slack_id": "U08DX2PBE6T",
        "position": "developer"
    },
    "Jine99": {
        "name": "김진",
        "role": "컨텐츠 개발",
        "branch_suffix": "JKim",
        "slack_id": "U08E92L3Z7F",
        "position": "developer"
    },
    "hyeonji9178": {
        "name": "김현지",
        "role": "컨텐츠 개발",
        "branch_suffix": "HJKim",
        "slack_id": "U08EC2QGW3C",
        "position": "developer"
    },
    "Rjcode7387": {
        "name": "류지형",
        "role": "컨텐츠 개발", 
        "branch_suffix": "JHRYU",
        "slack_id": "U08E92LAQ4D",
        "position": "developer"
    },
    "ppoyammy": {
        "name": "이은영",
        "role": "PM",
        "branch_suffix": "EYLEE",
        "slack_id": "U08EEEYNAF6",
        "position": "pm"
    },
    "yoonsung9999": {
        "name": "김윤성",
        "role": "기획자",
        "branch_suffix": "YSKIM",
        "slack_id": "U08EBL995D1",
        "position": "designer"
    },
    "rkaQu": {
        "name": "최민석",
        "role": "기획자",
        "branch_suffix": "MSCHOI",
        "slack_id": "U08E92L7KCM",
        "position": "designer"
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