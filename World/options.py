from dataclasses import dataclass

from Options import Choice, OptionGroup, PerGameCommonOptions, Range, Toggle

class GoalPercentage(Range):
    """
    What percentage of stars the player will have to beat in order for the game to be released
    """
    display_name = "Goal Star Percentage"
    range_start = 10
    range_end = 100
    default = 70

# This gets turned into the options, refers to classes above
@dataclass
class PPTOptions(PerGameCommonOptions):
    goal_percentage: GoalPercentage
